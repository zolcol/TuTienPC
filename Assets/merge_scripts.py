import os

scripts_dir = r"D:\Unity Project\test1\Assets\Scripts"
output_file = r"D:\Unity Project\test1\all_scripts.txt"

with open(output_file, "w", encoding="utf-8") as outfile:
    for root, _, files in os.walk(scripts_dir):
        for file in files:
            if file.endswith(".cs"):
                file_path = os.path.join(root, file)
                outfile.write(f"\n{'='*50}\n// FILE: {file_path}\n{'='*50}\n\n")
                with open(file_path, "r", encoding="utf-8", errors="ignore") as infile:
                    outfile.write(infile.read())
                outfile.write("\n")

print(f"Done! Exported to {output_file}")
