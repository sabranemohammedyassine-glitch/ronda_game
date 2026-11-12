r"""Regenerate every Zellige asset and review image in one run.

Usage:  python UI-Design/Generated/tools/build_all.py
Order: background -> cards (all 40 + back + suits + blank) -> UI kit -> UI kit 2 -> review images.
Requires Python 3 + Pillow; uses C:\Windows\Fonts\cambriab.ttf for card numerals.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
for step in [["gen_background.py"], ["gen_cards.py", "--all"], ["gen_ui.py"], ["gen_ui_extra.py"], ["gen_mockup.py"]]:
    print("==", " ".join(step))
    subprocess.run([sys.executable, os.path.join(HERE, step[0])] + step[1:], check=True, cwd=HERE)
