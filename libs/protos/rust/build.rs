use std::io::Result;

fn main() -> Result<()> {
    prost_build::Config::new()
        .type_attribute(".", "#[derive(serde::Serialize, serde::Deserialize)]")
        .protoc_arg("--experimental_allow_proto3_optional")
        .compile_protos(
            &["../events.proto", "../auth.proto", "../user.proto"],
            &[".."],
        )?;
    Ok(())
}
