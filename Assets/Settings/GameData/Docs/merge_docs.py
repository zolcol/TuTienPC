import re
import sys
from pathlib import Path

# Đảm bảo console Windows in ký tự UTF-8 không lỗi
if sys.platform == "win32":
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass

SCRIPT_DIR = Path(__file__).resolve().parent
DOCS_DIR = SCRIPT_DIR if SCRIPT_DIR.name == "Docs" else Path(r"Settings/GameData/Docs").resolve()

MASTER_FILE = DOCS_DIR / "DATA_CONVENTIONS_V2.md"
OUTPUT_FILE = Path(r"D:\Unity Project\test1\MERGED_DATA_CONVENTIONS.txt")

def merge_markdown_docs():
    if not MASTER_FILE.exists():
        print(f"[Error] Khong tim thay master file: {MASTER_FILE}")
        return

    master_text = MASTER_FILE.read_text(encoding="utf-8")

    # Tìm tất cả tên file .md được nhắc đến trong văn bản
    raw_matches = re.findall(r"[\w\-./\\]+\.md", master_text)
    
    # Lấy danh sách tên file duy nhất theo thứ tự xuất hiện
    referenced_filenames = []
    for match in raw_matches:
        fname = Path(match).name
        if fname != MASTER_FILE.name and fname not in referenced_filenames:
            referenced_filenames.append(fname)

    # Gom danh sách đường dẫn thực tế
    files_to_merge = [MASTER_FILE]
    for fname in referenced_filenames:
        found_path = next(DOCS_DIR.rglob(fname), None)
        if found_path and found_path.is_file():
            files_to_merge.append(found_path)
        else:
            print(f"[Warning] Khong tim thay file '{fname}' trong {DOCS_DIR}")

    # Gộp toàn bộ vào file output
    with open(OUTPUT_FILE, "w", encoding="utf-8") as out:
        for file_path in files_to_merge:
            separator = f"\n\n{'=' * 80}\n# TAP TIN: {file_path.name}\n# DUONG DAN: {file_path.as_posix()}\n{'=' * 80}\n\n"
            out.write(separator)
            out.write(file_path.read_text(encoding="utf-8"))

    print(f"[Success] Da gop thanh cong {len(files_to_merge)} file .md vao:\n{OUTPUT_FILE}")

if __name__ == "__main__":
    merge_markdown_docs()
