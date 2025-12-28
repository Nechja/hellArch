pub mod events {
    include!(concat!(env!("OUT_DIR"), "/hellarch.events.v1.rs"));
}

pub mod auth {
    include!(concat!(env!("OUT_DIR"), "/hellarch.auth.v1.rs"));
}

pub mod user {
    include!(concat!(env!("OUT_DIR"), "/hellarch.user.v1.rs"));
}
