/*
 * Copyright (c) 2006 Erik A. Brandstadmoen (C#/.NET port modifications/adaptations).
 *
 * UNICODE_BIDI_RUST_SOURCE_PATH: src/char_data/mod.rs; src/lib.rs; src/utf16.rs
 * UNICODE_BIDI_RUST_VERSION: 0.3.18
 * PORT_MODE: infrastructure
 */

use criterion::{Criterion, Throughput, criterion_group, criterion_main};
use serde::Deserialize;
use std::hint::black_box;
use std::time::Duration;

#[derive(Deserialize)]
#[serde(rename_all = "camelCase")]
struct Workloads {
    bidi_class_code_points: Vec<u32>,
    ltr_fragment: String,
    mixed_fragment: String,
    repeat_count: usize,
}

fn benchmarks(criterion: &mut Criterion) {
    let workloads: Workloads = serde_json::from_str(include_str!("../../workloads.json"))
        .expect("benchmark workloads must be valid JSON");
    let bidi_class_chars: Vec<char> = workloads
        .bidi_class_code_points
        .iter()
        .map(|value| char::from_u32(*value).expect("workload must contain Unicode scalar values"))
        .collect();
    let ltr_text: Vec<u16> = workloads
        .ltr_fragment
        .repeat(workloads.repeat_count)
        .encode_utf16()
        .collect();
    let mixed_text: Vec<u16> = workloads
        .mixed_fragment
        .repeat(workloads.repeat_count)
        .encode_utf16()
        .collect();

    let mut class_group = criterion.benchmark_group("bidi_class_lookup");
    class_group.throughput(Throughput::Elements(bidi_class_chars.len() as u64));
    class_group.bench_function("rust", |bencher| {
        bencher.iter(|| {
            let mut checksum = 0usize;
            for ch in &bidi_class_chars {
                checksum += unicode_bidi::bidi_class(black_box(*ch)) as usize;
            }
            black_box(checksum)
        });
    });
    class_group.finish();

    criterion.bench_function("initial_info_mixed", |bencher| {
        bencher.iter(|| unicode_bidi::utf16::InitialInfo::new(black_box(&mixed_text), None));
    });

    criterion.bench_function("bidi_info_ltr", |bencher| {
        bencher.iter(|| unicode_bidi::utf16::BidiInfo::new(black_box(&ltr_text), None));
    });

    criterion.bench_function("bidi_info_mixed", |bencher| {
        bencher.iter(|| unicode_bidi::utf16::BidiInfo::new(black_box(&mixed_text), None));
    });

    criterion.bench_function("bidi_info_reorder_mixed", |bencher| {
        bencher.iter(|| {
            let info = unicode_bidi::utf16::BidiInfo::new(black_box(&mixed_text), None);
            let mut checksum = 0usize;
            for paragraph in &info.paragraphs {
                checksum += info
                    .reorder_line(paragraph, paragraph.range.clone())
                    .len();
            }
            black_box(checksum)
        });
    });
}

criterion_group! {
    name = benches;
    config = Criterion::default()
        .warm_up_time(Duration::from_secs(3))
        .measurement_time(Duration::from_secs(5));
    targets = benchmarks
}
criterion_main!(benches);
