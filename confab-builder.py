import os
import argparse
from pathlib import Path
import shutil

confab_dir = Path("./Confab")
confab_UI_dir = Path("./ConfabUI")
confab_emails_dir = Path("./email_designs")

confab_build_dir = Path("./App")

def is_tool(name):
    """Check whether `name` is on PATH and marked as executable."""
    from shutil import which
    return which(name) is not None

def build_ui():
    from ConfabUI.build import build as buildUI
    buildUI()
    
def ignore_files(dir, files):
    ignore_list = []

    if(os.path.exists(confab_build_dir / "appsettings.json")): ignore_list.append('appsettings.json')

    return [file for file in files if file in ignore_list]

def build_backend(clean = False, platform = ""):
    abspath = os.path.abspath(__file__)
    current_dir = os.path.dirname(abspath)
    os.chdir(str(current_dir))

    if(not os.path.exists(confab_dir / "App_Data")): os.mkdir(confab_dir / "App_Data")
    if(not os.path.exists(confab_dir / "App_Data/email_templates")): os.mkdir(confab_dir / "App_Data/email_templates")

    from email_designs.compile import compile as compile_emails
    compile_emails()

    if(not is_tool("dotnet")):
        print("dotnet devtools is required to build Confab backend, but it doesn't seem to be installed. Install dotnet and try again")
        print("See https://docs.confabcomments.com/development/#prerequisites")
        exit()

    if(clean):
        os.system("dotnet clean")

    print("Building Confab backend...")

    abspath = os.path.abspath(__file__)
    current_dir = os.path.dirname(abspath)
    os.chdir(str(current_dir / confab_dir))

    if platform == "" or platform == None:
        platform_arg = ""
    else:
        platform_arg = "--runtime " + platform

    bundle_runtime = "--self-contained true" if args.bundle_runtime else "--self-contained false"
        
    os.system(f"dotnet publish --configuration Release --output build {platform_arg} {bundle_runtime}")
    
    os.chdir(str(current_dir))

    shutil.copytree(Path("./Confab/build"), confab_build_dir, dirs_exist_ok = True, ignore=ignore_files)
    shutil.copytree(Path("./Confab/scripts"), confab_build_dir, dirs_exist_ok = True)

    if(os.path.exists(Path("./Confab/build"))): shutil.rmtree(Path("./Confab/build"))

    print("\n✅✅✅ Building Confab backend complete! ✅✅✅")
    print("Output files are in ./App/\n")

def transfer_ui_to_backend():
    abspath = os.path.abspath(__file__)
    current_dir = os.path.dirname(abspath)
    os.chdir(str(current_dir))
    
    if(not os.path.exists(confab_dir / "wwwroot")): os.mkdir(confab_dir / "wwwroot")

    confab_UI_build_dir = confab_UI_dir / "dist"
    confab_backend_UI_files_dir = confab_dir / "wwwroot"

    shutil.copytree(confab_UI_build_dir, confab_backend_UI_files_dir, dirs_exist_ok = True)

if __name__ == "__main__":
    parser = argparse.ArgumentParser("confab_builder")

    parser.add_argument("-m", "--mode", choices=["ui", "backend", "full"], default="full", help="Choose whether to build UI, backend, or both")
    parser.add_argument("-c", "--clean", action="store_true", help="Clean build")
    parser.add_argument("-p", "--platform", help="Compile .NET backend for specific platform (leave empty for current platform).")
    parser.add_argument("-b", "--bundle-runtime", action="store_true", help="Bundle .NET runtime (will run without requiring .NET runtime to be installed, but will increase build size)")

    args = parser.parse_args()

    abspath = os.path.abspath(__file__)
    current_dir = os.path.dirname(abspath)
    os.chdir(str(current_dir))

    if(args.clean):
        print("Cleaning build files")
        if(os.path.exists(confab_UI_dir / "node_modules")): shutil.rmtree(confab_UI_dir / "node_modules")
        if(os.path.exists(confab_UI_dir / "dist")): shutil.rmtree(confab_UI_dir / "dist")
        if(os.path.exists(confab_dir / "App_Data")): shutil.rmtree(confab_dir / "App_Data")
        if(os.path.exists(confab_dir / "bin")): shutil.rmtree(confab_dir / "bin")
        if(os.path.exists(confab_dir / "build")): shutil.rmtree(confab_dir / "build")
        if(os.path.exists(confab_dir / "wwwroot")): shutil.rmtree(confab_dir / "wwwroot")
        if(os.path.exists(confab_emails_dir / "node_modules")): shutil.rmtree(confab_emails_dir / "node_modules")

    if(args.mode == "full" or args.mode == "ui"):
        build_ui()
    if(args.mode == "full"):
        transfer_ui_to_backend()
    if(args.mode == "full" or args.mode == "backend"):
        build_backend(args.clean, args.platform)

