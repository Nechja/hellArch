namespace User.Infrastructure

#nowarn "3261" // Nullness warning
#nowarn "3262" // Value known to be without null
#nowarn "3264" // Downcasting nullness

open System
open System.Collections.Generic
open System.Diagnostics
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open OpenTelemetry
open OpenTelemetry.Context.Propagation
open RabbitMQ.Client
open RabbitMQ.Client.Events
open User.Domain

module Events =

    let private activitySource = new ActivitySource("hellarch/user/events")
    let private propagator = Propagators.DefaultTextMapPropagator

    // Helper to extract trace context from RabbitMQ headers
    let private extractTraceContext (headers: IDictionary<string, obj>) =
        let getter =
            TextMapPropagator.Getter<IDictionary<string, obj>>(fun carrier key ->
                match carrier.TryGetValue(key) with
                | true, value when not (isNull value) ->
                    match value with
                    | :? (byte array) as bytes -> seq { Encoding.UTF8.GetString(bytes) }
                    | :? string as s -> seq { s }
                    | _ -> Seq.empty
                | _ -> Seq.empty)
        propagator.Extract(Baggage.Current, headers, getter)

    [<CLIMutable>]
    type CloudEvent<'T> =
        { specversion: string
          id: string
          source: string
          ``type``: string
          datacontenttype: string
          time: string
          data: 'T }

    [<CLIMutable>]
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

    let private handleUserCreated (dbConfig: Database.DatabaseConfig) (data: UserCreatedData) =
        task {
            let authUserId = Guid.Parse(data.user_id)

            // Check if profile already exists for this auth user
            let! existingProfile = Database.getProfileByAuthUserId dbConfig authUserId |> Async.StartAsTask

            match existingProfile with
            | Ok(Some _) ->
                printfn "Profile already exists for auth user %s, skipping" data.user_id
            | Ok None ->
                // Create the profile
                let request: CreateProfileFromEvent =
                    { AuthUserId = authUserId
                      Email = data.email
                      DisplayName = Some data.username
                      AvatarUrl =
                        if String.IsNullOrEmpty(data.avatar_url) then
                            None
                        else
                            Some data.avatar_url }

                let! result = Database.createProfile dbConfig request |> Async.StartAsTask

                match result with
                | Ok profile ->
                    printfn "Created profile %A for auth user %s" profile.Id data.user_id
                | Error err -> printfn "Failed to create profile: %A" err
            | Error err -> printfn "Failed to check existing profile: %A" err
        }

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

            let handler =
                AsyncEventHandler<BasicDeliverEventArgs>(fun _ args ->
                    task {
                        // Extract trace context from headers
                        let parentContext =
                            match args.BasicProperties.Headers with
                            | null -> PropagationContext.Empty
                            | headers ->
                                let dict = Dictionary<string, obj>()
                                for kvp in headers do
                                    dict.[kvp.Key] <- kvp.Value
                                extractTraceContext dict

                        // Start a span for this message
                        use activity =
                            activitySource.StartActivity(
                                "rabbitmq.consume",
                                ActivityKind.Consumer,
                                parentContext.ActivityContext
                            )

                        try
                            let body = Encoding.UTF8.GetString(args.Body.ToArray())
                            printfn "Received event: %s" body

                            let eventType =
                                match args.BasicProperties.Headers with
                                | null -> None
                                | headers ->
                                    match headers.TryGetValue("ce-type") with
                                    | true, value when not (isNull value) ->
                                        Some(Encoding.UTF8.GetString(value :?> byte array))
                                    | _ -> None

                            if not (isNull activity) then
                                activity.SetTag("messaging.system", "rabbitmq") |> ignore
                                activity.SetTag("messaging.destination.name", "hellarch.events") |> ignore
                                match eventType with
                                | Some et -> activity.SetTag("messaging.cloudevents.type", et) |> ignore
                                | None -> ()

                            match eventType with
                            | Some "hellarch.auth.user.created.v1" ->
                                try
                                    let event = JsonSerializer.Deserialize<CloudEvent<UserCreatedData>>(body, jsonOptions)
                                    do! handleUserCreated dbConfig event.data
                                with ex ->
                                    printfn "Failed to deserialize event: %s" ex.Message
                                    if not (isNull activity) then
                                        activity.SetStatus(ActivityStatusCode.Error, ex.Message) |> ignore

                            | Some eventType -> printfn "Ignoring event type: %s" eventType
                            | None -> printfn "Event missing type header"

                            do! channel.BasicAckAsync(args.DeliveryTag, multiple = false).AsTask()
                        with ex ->
                            printfn "Error processing event: %s" ex.Message
                            if not (isNull activity) then
                                activity.SetStatus(ActivityStatusCode.Error, ex.Message) |> ignore
                            // Nack and requeue on error
                            do! channel.BasicNackAsync(args.DeliveryTag, multiple = false, requeue = true).AsTask()
                    })

            consumer.add_ReceivedAsync(handler)

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
