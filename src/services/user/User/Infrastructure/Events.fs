namespace User.Infrastructure

open System
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open RabbitMQ.Client
open RabbitMQ.Client.Events
open User.Domain

module Events =

    type CloudEvent<'T> =
        { specversion: string
          id: string
          source: string
          ``type``: string
          datacontenttype: string
          time: string
          data: 'T }

    type UserCreatedData =
        { user_id: string
          email: string
          username: string
          provider: string
          provider_user_id: string
          avatar_url: string
          created_at: string }

    type EventsConfig =
        { RabbitMQUrl: string
          QueueName: string }

    let private jsonOptions =
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true
        options

    let startConsumer
        (config: EventsConfig)
        (dbConfig: Database.DatabaseConfig)
        (cancellationToken: CancellationToken)
        =
        task {
            let factory = ConnectionFactory(Uri = Uri(config.RabbitMQUrl))
            let! connection = factory.CreateConnectionAsync(cancellationToken)
            let! channel = connection.CreateChannelAsync(cancellationToken = cancellationToken)

            // Declare exchange (must match publisher)
            do!
                channel.ExchangeDeclareAsync(
                    exchange = "hellarch.events",
                    ``type`` = ExchangeType.Topic,
                    durable = true,
                    autoDelete = false,
                    arguments = null,
                    cancellationToken = cancellationToken
                )

            // Declare queue for this service
            let! _ =
                channel.QueueDeclareAsync(
                    queue = config.QueueName,
                    durable = true,
                    exclusive = false,
                    autoDelete = false,
                    arguments = null,
                    cancellationToken = cancellationToken
                )

            // Bind to user events
            do!
                channel.QueueBindAsync(
                    queue = config.QueueName,
                    exchange = "hellarch.events",
                    routingKey = "hellarch.auth.user.#",
                    arguments = null,
                    cancellationToken = cancellationToken
                )

            printfn "Connected to RabbitMQ, consuming from queue: %s" config.QueueName

            let consumer = AsyncEventingBasicConsumer(channel)

            consumer.ReceivedAsync.Add(fun args ->
                task {
                    try
                        let body = Encoding.UTF8.GetString(args.Body.ToArray())
                        printfn "Received event: %s" body

                        let eventType =
                            match args.BasicProperties.Headers with
                            | null -> None
                            | headers ->
                                match headers.TryGetValue("ce-type") with
                                | true, value -> Some(Encoding.UTF8.GetString(value :?> byte[]))
                                | false, _ -> None

                        match eventType with
                        | Some "hellarch.auth.user.created.v1" ->
                            let event =
                                JsonSerializer.Deserialize<CloudEvent<UserCreatedData>>(body, jsonOptions)

                            let userId = Guid.Parse(event.data.user_id)

                            // Check if profile already exists
                            let! existingProfile = Database.getProfileOption dbConfig userId |> Async.StartAsTask

                            match existingProfile with
                            | Ok(Some _) ->
                                printfn "Profile already exists for user %s, skipping" event.data.user_id
                            | Ok None ->
                                // Create the profile
                                let request: CreateProfileRequest =
                                    { UserId = userId
                                      Email = event.data.email
                                      DisplayName = Some event.data.username
                                      AvatarUrl =
                                        if String.IsNullOrEmpty(event.data.avatar_url) then
                                            None
                                        else
                                            Some event.data.avatar_url }

                                let! result = Database.createProfile dbConfig request |> Async.StartAsTask

                                match result with
                                | Ok profile ->
                                    printfn "Created profile for user %s: %A" event.data.user_id profile.Id
                                | Error err -> printfn "Failed to create profile: %A" err
                            | Error err -> printfn "Failed to check existing profile: %A" err

                        | Some eventType -> printfn "Ignoring event type: %s" eventType
                        | None -> printfn "Event missing type header"

                        do! channel.BasicAckAsync(args.DeliveryTag, multiple = false)
                    with ex ->
                        printfn "Error processing event: %s" ex.Message
                        // Nack and requeue on error
                        do! channel.BasicNackAsync(args.DeliveryTag, multiple = false, requeue = true)
                }
                |> ValueTask)

            let! _ =
                channel.BasicConsumeAsync(
                    queue = config.QueueName,
                    autoAck = false,
                    consumer = consumer,
                    cancellationToken = cancellationToken
                )

            // Keep running until cancelled
            while not cancellationToken.IsCancellationRequested do
                do! Task.Delay(1000, cancellationToken)

            do! channel.CloseAsync(cancellationToken)
            do! connection.CloseAsync(cancellationToken)
        }
