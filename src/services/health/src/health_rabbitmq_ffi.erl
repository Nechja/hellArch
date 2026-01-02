-module(health_rabbitmq_ffi).
-export([parse_consumer_count/1]).

%% Parse consumer count from RabbitMQ management API response
%% Simple regex-based parsing to avoid JSON library dependencies
parse_consumer_count(JsonBin) ->
    %% Look for "consumers":N pattern in the JSON
    case re:run(JsonBin, <<"\"consumers\":(\\d+)">>, [{capture, [1], binary}]) of
        {match, [CountBin]} ->
            {ok, binary_to_integer(CountBin)};
        nomatch ->
            {error, nil}
    end.
