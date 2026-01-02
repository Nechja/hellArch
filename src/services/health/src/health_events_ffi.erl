-module(health_events_ffi).
-export([http_post/3, generate_uuid/0]).

http_post(Url, Body, TimeoutMs) ->
    application:ensure_all_started(inets),
    application:ensure_all_started(ssl),
    UrlStr = binary_to_list(Url),
    BodyStr = binary_to_list(Body),
    Headers = [{"Content-Type", "application/json"}],
    Request = {UrlStr, Headers, "application/json", BodyStr},
    case httpc:request(post, Request, [{timeout, TimeoutMs}], [{body_format, binary}]) of
        {ok, {{_, StatusCode, _}, _, _}} ->
            {ok, StatusCode};
        {error, Reason} ->
            ReasonBin = list_to_binary(io_lib:format("~p", [Reason])),
            {error, ReasonBin}
    end.

generate_uuid() ->
    %% Simple UUID v4 generation without external deps
    <<A:32, B:16, C:16, D:16, E:48>> = crypto:strong_rand_bytes(16),
    %% Set version to 4 and variant to RFC 4122
    C2 = (C band 16#0fff) bor 16#4000,
    D2 = (D band 16#3fff) bor 16#8000,
    list_to_binary(io_lib:format("~8.16.0b-~4.16.0b-~4.16.0b-~4.16.0b-~12.16.0b",
        [A, B, C2, D2, E])).
