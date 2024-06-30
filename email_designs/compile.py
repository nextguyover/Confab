import os
from pathlib import Path

def is_tool(name):
    """Check whether `name` is on PATH and marked as executable."""
    from shutil import which
    return which(name) is not None

def compile(clean = False):
    abspath = os.path.abspath(__file__)     # changing to the directory of the script
    dname = os.path.dirname(abspath)
    os.chdir(dname)
    
    print("Compiling email templates...")

    mjml_path = Path("./node_modules/.bin/mjml")
    input_dir = Path("./templates/")
    compiled_file_output_dir = Path("../Confab/App_Data/email_templates/")

    if(not os.path.isfile(mjml_path)):
        print("mjml is not installed, attempting to install now...")
        os.system("npm install -y")

    if(not os.path.isfile(mjml_path)):
        print("Attempted install, but mjml executable couldn't be found, exiting...")
        exit()

    print("mjml executable is present. Attempting to compile files now. Up to date files will be skipped...")

    if(not os.path.exists(compiled_file_output_dir)): os.makedirs(compiled_file_output_dir)

    if(clean):
        import shutil
        shutil.rmtree(str(compiled_file_output_dir))
        os.mkdir(str(compiled_file_output_dir))

    if(not is_tool("node")):
        print("NodeJS is required to build email templates using MJML, but it doesn't seem to be installed. Install NodeJS and try again")
        print("See https://docs.confabcomments.com/development/#prerequisites")
        exit()

    for file in os.listdir(input_dir):
        if(file.endswith(".mjml")):
            input_filepath = str(input_dir / file)
            output_file = os.path.splitext(file)[0] + ".html"
            output_filepath = str(compiled_file_output_dir / output_file)

            #first check if html file exists, then, compare date modified, and compile only if .html file is older than .mjml file if(os.path.isfile(compiled_file_output_dir + os.path.splitext(file)[0] + ".html")):
            if(os.path.isfile(output_filepath) and os.path.getmtime(output_filepath) > os.path.getmtime(input_filepath)):
                # print(output_file + " is up to date, skipping")
                continue

            print("Compiling " + file)
            execute_str = "" + str(mjml_path) + " --config.minify true --config.beautify false " + str(input_dir / file) + " -o " + str(compiled_file_output_dir / os.path.splitext(file)[0]) + ".html"
            os.system(execute_str)

    print("Done!")

if __name__ == "__main__":
    compile()