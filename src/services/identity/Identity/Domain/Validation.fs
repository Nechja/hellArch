namespace Identity.Domain

open System
open System.Text.RegularExpressions

module Validation =

    let private emailRegex = Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)

    let validateEmail (email: string) =
        if String.IsNullOrWhiteSpace(email) then
            Error (IdentityError.ValidationError "Email is required")
        elif email.Length > 255 then
            Error (IdentityError.ValidationError "Email must be 255 characters or less")
        elif not (emailRegex.IsMatch(email)) then
            Error (IdentityError.ValidationError "Invalid email format")
        else
            Ok (email.ToLowerInvariant().Trim())

    let validateUsername (username: string) =
        if String.IsNullOrWhiteSpace(username) then
            Error (IdentityError.ValidationError "Username is required")
        elif username.Length < 2 then
            Error (IdentityError.ValidationError "Username must be at least 2 characters")
        elif username.Length > 255 then
            Error (IdentityError.ValidationError "Username must be 255 characters or less")
        else
            Ok (username.Trim())

    let validatePassword (password: string) =
        if String.IsNullOrWhiteSpace(password) then
            Error (IdentityError.ValidationError "Password is required")
        elif password.Length < 8 then
            Error (IdentityError.ValidationError "Password must be at least 8 characters")
        elif password.Length > 128 then
            Error (IdentityError.ValidationError "Password must be 128 characters or less")
        else
            Ok password

    let validateRegistration (req: RegistrationRequest) =
        match validateEmail req.Email with
        | Error e -> Error e
        | Ok email ->
            match validateUsername req.Username with
            | Error e -> Error e
            | Ok username ->
                match validatePassword req.Password with
                | Error e -> Error e
                | Ok password ->
                    Ok { req with Email = email; Username = username; Password = password }

    let validateRedirectUri (allowedUris: string list) (uri: string) =
        if List.contains uri allowedUris then
            Ok uri
        else
            Error IdentityError.InvalidRedirectUri
