-module(health_http_ffi).
-export([get/2, decode_ok/1, decode_error/1]).

get(Url, TimeoutMs) ->
    application:ensure_all_started(inets),
    application:ensure_all_started(ssl),
    UrlStr = binary_to_list(Url),
    case httpc:request(get, {UrlStr, []}, [{timeout, TimeoutMs}], [{body_format, binary}]) of
        {ok, {{_, StatusCode, _}, _, Body}} ->
            {ok, StatusCode, Body};
        {error, Reason} ->
            ReasonBin = list_to_binary(io_lib:format("~p", [Reason])),
            {error, ReasonBin}
    end.

decode_ok({ok, StatusCode, Body}) when is_integer(StatusCode), is_binary(Body) ->
    {ok, {StatusCode, Body}};
decode_ok(_) ->
    {error, nil}.

decode_error({error, Reason}) when is_binary(Reason) ->
    {ok, Reason};
decode_error(_) ->
    {error, nil}.
