use std::env;

fn main() {
    let args: Vec<String> = env::args().collect();
    if args.len() != 3 || args[1] != "bidi-class" {
        eprintln!("usage: cargo run --manifest-path tools/parity/rust_probe/Cargo.toml -- bidi-class <codepoint>");
        std::process::exit(2);
    }

    let code_point = parse_code_point(&args[2]).unwrap_or_else(|| {
        eprintln!("invalid codepoint: {}", args[2]);
        std::process::exit(2);
    });

    let ch = char::from_u32(code_point).unwrap_or_else(|| {
        eprintln!("invalid Unicode scalar value: {}", args[2]);
        std::process::exit(2);
    });

    println!("{:?}", unicode_bidi::bidi_class(ch));
}

fn parse_code_point(value: &str) -> Option<u32> {
    if let Some(hex) = value.strip_prefix("0x").or_else(|| value.strip_prefix("0X")) {
        u32::from_str_radix(hex, 16).ok()
    } else {
        value.parse().ok()
    }
}
