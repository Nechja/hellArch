namespace HellArch.CloudEvents

[<CLIMutable>]
type CloudEvent<'T> = {
    specversion: string
    id: string
    source: string
    ``type``: string
    datacontenttype: string
    time: string
    data: 'T
}
