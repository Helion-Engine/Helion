import os
import re
import sys


EXECUTE_COMMANDS = True
EXPECTED_WORKING_DIR = 'Scripts'
PREFIX_PATH = '..'
BRANCH_NAME = 'temp-move-file'

files_to_move = []


def exit_if(cond, msg):
    if cond:
        print(msg)
        sys.exit(1)


def execute(cmd):
    if EXECUTE_COMMANDS:
        os.system(cmd)
    else:
        print(cmd)


def in_script_dir():
    return os.getcwd().endswith(EXPECTED_WORKING_DIR)


def is_dir(path):
    return os.path.isdir(path)


def add_file(from_path, to_path):
    exit_if(not os.path.exists(from_path), f'Cannot find source path: {from_path}')
    exit_if(not os.path.exists(os.path.dirname(to_path)), f'Cannot find destination directory for: {to_path}')
    print(f'Moving {from_path} to directory {os.path.dirname(to_path)}')
    files_to_move.append((from_path, to_path))


def add_dir(from_path, to_path):
    exit_if(True, 'Directories not supported yet')
    # for root, dirs, files in os.walk(from_path, topdown=False):
    #     for name in files:
    #         print('FILE:', os.path.join(root, name))


def checkout_new_branch():
    exit_if(not re.fullmatch(r'[a-zA-Z0-9_-]+', BRANCH_NAME, flags=0), f'Invalid characters in branch name: {BRANCH_NAME}')
    execute(f'git checkout -b {BRANCH_NAME}')


def move_files_and_commit():
    for pair in files_to_move:
        from_path, to_path = pair
        execute(f'git mv {from_path} {to_path}')
    execute(f'git commit -m "Moving source files to new location with history preservation"')


def checkout_and_commit_old_files():
    for pair in files_to_move:
        from_path, _ = pair
        execute(f'git checkout HEAD~ {from_path}')
    execute(f'git commit -m "Restoring original source files with history preservation"')


def checkout_and_merge():
    execute(f'git checkout -')
    execute(f'git merge --no-ff {BRANCH_NAME}')


def delete_old_files():
    for pair in files_to_move:
        from_path, _ = pair
        execute(f'git rm {from_path}')
    execute(f'git commit -m "Fully removing all old source files"')


def populate_files_to_move(from_to_pair_list):
    for i in range(0, len(from_to_pair_list), 2):
        from_path, to_path = f'{PREFIX_PATH}/{from_to_pair_list[i]}', f'{PREFIX_PATH}/{from_to_pair_list[i+1]}'
        exit_if(is_dir(from_path) != is_dir(to_path), f'One path is a dir and the other is a file: {from_path} {to_path}')
        if is_dir(from_path):
            add_dir(from_path, to_path)
        else:
            add_file(from_path, to_path)

def delete_temp_branch():
    execute(f'git branch -D {BRANCH_NAME}')


# Example usage:
# git_mv.py "Core/Render/OpenGL/Vertex/Something.cs" "Core/Render/OpenGL/Moved.cs"
if __name__ == '__main__':
    exit_if(not in_script_dir(), f'Need to execute from the script directory, in {os.getcwd()} but need to be in {EXPECTED_WORKING_DIR}')
    exit_if(len(sys.argv) == 1, f'Usage example: {sys.argv[0]} Core/oldFilePathA Core/newFilePathA Core/Render/oldFileDirB Core/Render/newFileDirB...')
    exit_if(len(sys.argv) % 2 == 0, f'Uneven number of arguments, a source file/folder needs a destination')
    populate_files_to_move(sys.argv[1:])
    checkout_new_branch()
    move_files_and_commit()
    checkout_and_commit_old_files()
    checkout_and_merge()
    delete_old_files()
    delete_temp_branch()
