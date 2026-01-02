-module(health_rabbitmq_ffi).
-export([connect/1, publish/4, close/1]).

connect(Url) ->
    application:ensure_all_started(amqp_client),
    case amqp_uri:parse(binary_to_list(Url)) of
        {ok, AmqpParams} ->
            case amqp_connection:start(AmqpParams) of
                {ok, Connection} ->
                    case amqp_connection:open_channel(Connection) of
                        {ok, Channel} ->
                            % Declare exchange
                            ExchangeDeclare = #'exchange.declare'{
                                exchange = <<"hellarch.events">>,
                                type = <<"topic">>,
                                durable = true
                            },
                            amqp_channel:call(Channel, ExchangeDeclare),
                            {ok, {Connection, Channel}};
                        {error, Reason} ->
                            amqp_connection:close(Connection),
                            {error, list_to_binary(io_lib:format("~p", [Reason]))}
                    end;
                {error, Reason} ->
                    {error, list_to_binary(io_lib:format("~p", [Reason]))}
            end;
        {error, Reason} ->
            {error, list_to_binary(io_lib:format("~p", [Reason]))}
    end.

publish({_Connection, Channel}, Exchange, RoutingKey, Payload) ->
    Publish = #'basic.publish'{
        exchange = Exchange,
        routing_key = RoutingKey
    },
    Props = #'P_basic'{
        content_type = <<"application/json">>,
        delivery_mode = 2 % persistent
    },
    try
        amqp_channel:cast(Channel, Publish, #amqp_msg{props = Props, payload = Payload}),
        ok
    catch
        _:Reason ->
            {error, list_to_binary(io_lib:format("~p", [Reason]))}
    end.

close({Connection, Channel}) ->
    amqp_channel:close(Channel),
    amqp_connection:close(Connection),
    ok.
