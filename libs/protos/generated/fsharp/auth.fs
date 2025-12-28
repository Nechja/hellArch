namespace HellArch.Auth.V1

open System

type OAuthProvider =
    | OauthProviderDiscord
    | OauthProviderGoogle
    | OauthProviderGithub
    | OauthProviderMicrosoft

type GetOAuthUrlRequest = {
    Provider: OAuthProvider
    RedirectUri: string
    State: string
}

and GetOAuthUrlResponse = {
    AuthUrl: string
}

and HandleOAuthCallbackRequest = {
    Provider: OAuthProvider
    Code: string
    State: string
}

and HandleOAuthCallbackResponse = {
    AccessToken: string
    RefreshToken: string
    ExpiresIn: int64
    UserInfo: UserInfo option
}

and RefreshTokenRequest = {
    RefreshToken: string
}

and RefreshTokenResponse = {
    AccessToken: string
    RefreshToken: string
    ExpiresIn: int64
}

and RevokeTokenRequest = {
    Token: string
}

and RevokeTokenResponse = {
    Success: bool
}

and ValidateTokenRequest = {
    Token: string
}

and ValidateTokenResponse = {
    Valid: bool
    UserInfo: UserInfo option
    ExpiresAt: DateTime option
}

and GetSessionRequest = {
    SessionId: string
}

and GetSessionResponse = {
    Session: Session option
}

and EndSessionRequest = {
    SessionId: string
}

and EndSessionResponse = {
    Success: bool
}

and Session = {
    Id: string
    UserId: string
    CreatedAt: DateTime option
    ExpiresAt: DateTime option
    IpAddress: string
    UserAgent: string
}

and UserInfo = {
    Id: string
    ProviderId: string
    Provider: OAuthProvider
    Email: string
    Username: string
    AvatarUrl: string
    Claims: Map<string, ClaimsEntry>
}

