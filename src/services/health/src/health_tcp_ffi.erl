-module(health_tcp_ffi).
-export([check_port/3]).

check_port(Host, Port, TimeoutMs) ->
    HostStr = binary_to_list(Host),
    case gen_tcp:connect(HostStr, Port, [binary, {active, false}], TimeoutMs) of
        {ok, Socket} ->
            gen_tcp:close(Socket),
            true;
        {error, _} ->
            false
    end.
