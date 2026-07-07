# Upstream Unit Test Parity

Last updated: 2026-07-07

Target: Rust `unicode-bidi` `0.3.18` from `artifacts/upstream/unicode-bidi-0.3.18`.

## Ported Unit Tests

| Rust source | Rust test | .NET coverage |
|---|---|---|
| `src/char_data/mod.rs` | `test_ascii` | `CharDataTests.TestAscii` |
| `src/char_data/mod.rs` | `test_bmp` | `CharDataTests.TestBmp` |
| `src/char_data/mod.rs` | `test_smp` | `CharDataTests.TestSmp` |
| `src/char_data/mod.rs` | `test_unassigned_planes` | `CharDataTests.TestUnassignedPlanes` |
| `src/level.rs` | `test_new` | `levelTests.TestNew` |
| `src/level.rs` | `test_new_explicit` | `levelTests.TestNewExplicit` |
| `src/level.rs` | `test_is_ltr` | `levelTests.TestIsLtr` |
| `src/level.rs` | `test_is_rtl` | `levelTests.TestIsRtl` |
| `src/level.rs` | `test_raise` | `levelTests.TestRaise` |
| `src/level.rs` | `test_raise_explicit` | `levelTests.TestRaiseExplicit` |
| `src/level.rs` | `test_lower` | `levelTests.TestLower` |
| `src/level.rs` | `test_has_rtl` | `levelTests.TestHasRtl` |
| `src/level.rs` | `test_into` | `levelTests.TestInto` |
| `src/level.rs` | `test_vec` | `levelTests.TestVec` |
| `src/level.rs` | `test_str_eq` | `levelTests.TestStrEq` |
| `src/level.rs` | `test_string_eq` | `levelTests.TestStringEq` |
| `src/prepare.rs` | `test_level_runs` | `prepareTests.LevelRuns_MatchesRustExamples` |
| `src/prepare.rs` | `test_isolating_run_sequences` | `prepareTests.IsolatingRunSequences_*` |
| `src/prepare.rs` | `test_isolating_run_sequences_sos_and_eos` | `prepareTests.IsolatingRunSequences_SosAndEos*` |
| `src/prepare.rs` | `test_removed_by_x9` | `prepareTests.RemovedByX9_MatchesRustPredicate` |
| `src/prepare.rs` | `test_not_removed_by_x9` | `prepareTests.NotRemovedByX9_MatchesRustPredicate` |
| `src/lib.rs` | `test_utf16_text_source` | `libTests.TestUtf16TextSource` |
| `src/lib.rs` | `test_utf16_char_iter` | `libTests.TestUtf16CharIter` |
| `src/lib.rs` | `test_initial_text_info` | `libTests.TestInitialTextInfo` |
| `src/lib.rs` | `test_process_text` | `libTests.TestProcessText` |
| `src/lib.rs` | `test_paragraph_bidi_info` | `libTests.TestParagraphBidiInfo` |
| `src/lib.rs` | `test_bidi_info_has_rtl` | `libTests.TestBidiInfoHasRtl` |
| `src/lib.rs` | `test_reorder_line` | `libTests.TestReorderLine` |
| `src/lib.rs` | `test_reordered_levels_range` | `libTests.TestReorderedLevelsRange` |
| `src/lib.rs` | `test_reordered_levels` | `libTests.TestReorderedLevels` |
| `src/lib.rs` | `test_paragraph_info_len` | `libTests.TestParagraphInfoLen` |
| `src/lib.rs` | `test_direction` | `libTests.TestDirection` |
| `src/lib.rs` | `test_edge_cases_direction` | `libTests.TestEdgeCasesDirection` |
| `src/lib.rs` | `test_level_at` | `libTests.TestLevelAt` |
| `src/lib.rs` | `test_get_base_direction` | `libTests.TestGetBaseDirection` |
| `src/lib.rs` | `test_get_base_direction_full` | `libTests.TestGetBaseDirectionFull` |

## Rust-Only Feature Tests

The upstream serde feature tests are not ported because this .NET package does not expose Rust `serde` serialization.

| Rust source | Rust test | Reason |
|---|---|---|
| `src/level.rs` | `serde_tests::test_statics` | Rust `serde` token stream assertion. |
| `src/level.rs` | `serde_tests::test_new` | Rust `serde` token stream assertion. |
| `src/lib.rs` | `serde_tests::test_levels` | Rust `serde` token stream assertion. |
