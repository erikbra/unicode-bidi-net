import unittest

from generate_char_data import ucd_pairs, ucd_ranges


class UcdGenerationTests(unittest.TestCase):
    def test_explicit_assignments_override_regional_and_global_defaults(self):
        source = """# @missing: 0000..10FFFF; Left_To_Right
# @missing: 0590..05FF; Right_To_Left
# @missing: 0600..07BF; Arabic_Letter
# @missing: 20A0..20CF; European_Terminator
05C8; NSM
0600; AN
FDD0..FDEF; BN
"""
        self.assertEqual(
            [(0x0590, 0x05C7, "R"), (0x05C8, 0x05C8, "NSM"),
             (0x05C9, 0x05FF, "R"), (0x0600, 0x0600, "AN"),
             (0x0601, 0x07BF, "AL"), (0x20A0, 0x20CF, "ET"),
             (0xFDD0, 0xFDEF, "BN")],
            ucd_ranges(source),
        )

    def test_unknown_bidi_classes_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "Unknown bidi class"):
            ucd_ranges("# @missing: 0000..10FFFF; Left_To_Right\n0041; TYPO")

    def test_empty_data_cannot_generate_an_all_ltr_table(self):
        with self.assertRaisesRegex(ValueError, "Expected UCD"):
            ucd_ranges("")

    def test_canonically_equivalent_brackets_share_an_opening(self):
        brackets = "2329; 232A; o\n232A; 2329; c\n3008; 3009; o\n3009; 3008; c"
        # UnicodeData field 5 contains the canonical decomposition.
        data = "2329;LEFT-POINTING ANGLE BRACKET;Ps;0;ON;3008;;;;N;;;;;\n"
        self.assertEqual([(0x2329, 0x232A, 0x3008), (0x3008, 0x3009, None)], ucd_pairs(brackets, data))

    def test_one_sided_bracket_pairs_are_rejected(self):
        with self.assertRaisesRegex(ValueError, "Non-reciprocal"):
            ucd_pairs("0028; 0029; o", "")


if __name__ == "__main__":
    unittest.main()
