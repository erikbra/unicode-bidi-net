use std::env;

fn main() {
    let args: Vec<String> = env::args().collect();
    match args.get(1).map(String::as_str) {
        Some("bidi-class") if args.len() == 3 => print_bidi_class(&args[2]),
        Some("initial-info") if args.len() == 4 => print_initial_info(&args[2], &args[3]),
        Some("bidi-info") if args.len() == 4 => print_bidi_info(&args[2], &args[3]),
        Some("reorder-line") if args.len() == 4 => print_reorder_line(&args[2], &args[3]),
        _ => {
            eprintln!("usage:");
            eprintln!("  cargo run --manifest-path tools/parity/rust_probe/Cargo.toml -- bidi-class <codepoint>");
            eprintln!("  cargo run --manifest-path tools/parity/rust_probe/Cargo.toml -- initial-info <utf16-code-units> <auto|ltr|rtl|number>");
            eprintln!("  cargo run --manifest-path tools/parity/rust_probe/Cargo.toml -- bidi-info <utf16-code-units> <auto|ltr|rtl|number>");
            eprintln!("  cargo run --manifest-path tools/parity/rust_probe/Cargo.toml -- reorder-line <utf16-code-units> <auto|ltr|rtl|number>");
            std::process::exit(2);
        }
    }
}

fn parse_code_point(value: &str) -> Option<u32> {
    if let Some(hex) = value
        .strip_prefix("0x")
        .or_else(|| value.strip_prefix("0X"))
    {
        u32::from_str_radix(hex, 16).ok()
    } else {
        value.parse().ok()
    }
}

fn print_bidi_class(value: &str) {
    let code_point = parse_code_point(value).unwrap_or_else(|| {
        eprintln!("invalid codepoint: {}", value);
        std::process::exit(2);
    });

    let ch = char::from_u32(code_point).unwrap_or_else(|| {
        eprintln!("invalid Unicode scalar value: {}", value);
        std::process::exit(2);
    });

    println!("{:?}", unicode_bidi::bidi_class(ch));
}

fn print_initial_info(units: &str, default_level: &str) {
    let text = parse_utf16_units(units);
    let initial = unicode_bidi::utf16::InitialInfo::new(&text, parse_default_level(default_level));
    println!("{}", format_initial_info(&initial));
}

fn print_bidi_info(units: &str, default_level: &str) {
    let text = parse_utf16_units(units);
    let bidi = unicode_bidi::utf16::BidiInfo::new(&text, parse_default_level(default_level));
    let initial = unicode_bidi::utf16::InitialInfo::new(&text, parse_default_level(default_level));
    let levels = bidi
        .levels
        .iter()
        .map(|level| level.number().to_string())
        .collect::<Vec<_>>()
        .join(",");

    println!("{};levels={}", format_initial_info(&initial), levels);
}

fn print_reorder_line(units: &str, default_level: &str) {
    let text = parse_utf16_units(units);
    let bidi = unicode_bidi::utf16::BidiInfo::new(&text, parse_default_level(default_level));
    let lines = bidi
        .paragraphs
        .iter()
        .map(|paragraph| {
            let reordered = bidi.reorder_line(paragraph, paragraph.range.clone());
            format_utf16_units(&reordered)
        })
        .collect::<Vec<_>>()
        .join("|");

    println!("lines={}", lines);
}

fn format_initial_info(initial: &unicode_bidi::utf16::InitialInfo) -> String {
    let classes = initial
        .original_classes
        .iter()
        .map(|class| format!("{:?}", class))
        .collect::<Vec<_>>()
        .join(",");
    let paragraphs = initial
        .paragraphs
        .iter()
        .map(|paragraph| {
            format!(
                "{}-{}:{}",
                paragraph.range.start,
                paragraph.range.end,
                paragraph.level.number()
            )
        })
        .collect::<Vec<_>>()
        .join(",");

    format!("classes={};paragraphs={}", classes, paragraphs)
}

fn format_utf16_units(units: &[u16]) -> String {
    units
        .iter()
        .map(|unit| format!("{:04X}", unit))
        .collect::<Vec<_>>()
        .join(",")
}

fn parse_utf16_units(value: &str) -> Vec<u16> {
    if value.trim().is_empty() {
        return Vec::new();
    }

    value
        .split(',')
        .map(|unit| u16::from_str_radix(unit.trim(), 16).expect("invalid UTF-16 code unit"))
        .collect()
}

fn parse_default_level(value: &str) -> Option<unicode_bidi::Level> {
    match value {
        "auto" => None,
        "ltr" => Some(unicode_bidi::Level::ltr()),
        "rtl" => Some(unicode_bidi::Level::rtl()),
        number => Some(
            unicode_bidi::Level::new(number.parse().expect("invalid level"))
                .expect("level out of range"),
        ),
    }
}
