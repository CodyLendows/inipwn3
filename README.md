# INIPWN3 - Enhanced INI File Manipulator

## Overview
INIPWN3 is a powerful command-line tool for viewing, editing, and managing INI configuration files. It provides an interactive, user-friendly (lol) interface with syntax highlighting, search functionality, and inline editing for seamless modification of INI files.

## Features
- **Load and Save INI Files**: Open existing INI files and save modifications effortlessly.
- **Cross-platform support**: Tested on both Windows and Linux systems, mediocre on both of them!
- **Syntax Highlighting**: Differentiates between sections, keys, and values for better readability.
- **Interactive Navigation**: Scroll through files, select options, and edit values dynamically.
- **Search Functionality**: Find and navigate between specific keys or values.
- **Inline Editing**: Modify key-value pairs directly within the editor.
- **File Selection Interface**: Easily browse and choose INI files from the current directory.

## Installation
### Prerequisites
Ensure you have the following installed on your system:
- [.NET SDK](https://dotnet.microsoft.com/en-us/download)

### Clone the Repository
```sh
git clone https://github.com/CodyLendows/inipwn3.git
cd inipwn3
```

### Build the Project
```sh
dotnet build
```

### Run the Application (Testing)
```sh
dotnet run
```

(You'd ideally want to compile it into a static exe and drop it into your $PATH somewhere for long term usage.)

To open a specific INI file:
```sh
./inipwn3 /var/exposure/exposure.ini
```

## Usage
### File Selection View
- `w` - Move selection up
- `s` - Move selection down
- `Enter` - Open selected file
- `q` - Quit the application

### Editor View
- `w` - Scroll up
- `s` - Scroll down
- `search <term>` - Find a term in the file
- `n` / `p` - Jump to next/previous search match
- `set <section> <key> <value>` - Modify a key's value
- `save <filepath>` - Save the INI file
- `edit` - Switch to inline editing mode
- `back` - Return to file selection
- `q` - Quit

### Inline Editor
- `w` / `s` - Move between lines
- `e` - Edit a key's value
- `back` - Return to editor view

## Contributing
1. Fork the repository
2. Create a new branch (`git checkout -b feature-branch`)
3. Commit your changes (`git commit -m "Add new feature"`)
4. Push to your branch (`git push origin feature-branch`)
5. Open a Pull Request

## License
This project is licensed under the MIT License. See `LICENSE` for details.

