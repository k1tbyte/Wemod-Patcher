# Contributing to WeMod Patcher

Thank you for your interest in the WeMod Patcher project! This document provides guidelines for contributing to the project.

## Table of Contents

- [Development Environment Setup](#development-environment-setup)
- [Bug Reports](#bug-reports)
- [Feature Suggestions](#feature-suggestions)
- [Creating a Pull Request](#creating-a-pull-request)
- [Code Style](#code-style)
- [Testing](#testing)
- [License](#license)

## Code of Conduct

By participating in this project, you commit to maintaining respectful interactions with all community members. Any form of insults, harassment, or other unacceptable behavior will not be tolerated.

## Project Structure

The project consists of the following main components:

- **WeModPatcher** - Main project containing the patcher logic and user interface
- **AsarSharp** - Library for working with ASAR archives (used for unpacking and modifying WeMod files)
- **Core** - Core of the patcher, including static and dynamic patching
- **Models** - Data models used in the project
- **View** - User interface components

## Development Environment Setup

1. Clone the repository:
   ```
   git clone https://github.com/k1tbyte/Wemod-Patcher.git
   ```

2. Open the solution `Wemod-Patcher.sln` in Visual Studio or JetBrains Rider.

3. Restore NuGet packages.

4. Build the project.

## Bug Reports

If you've found a bug, please create an Issue with a detailed description:

- WeMod Patcher version
- WeMod version where the problem occurred
- Detailed steps to reproduce the bug
- Expected and actual behavior
- Screenshots or error logs (if available)

## Feature Suggestions

Suggestions for new features or improvements are welcome! Create an Issue describing your idea, explaining:

- What problem the proposed improvement solves
- How you envision implementing this feature
- Potential alternatives you've considered

## Creating a Pull Request

1. Fork the repository.
2. Create a branch with a descriptive name:
   ```
   git checkout -b feature/feature-name
   ```
   or
   ```
   git checkout -b fix/fix-name
   ```

3. Make the necessary changes and commit with clear, descriptive messages.

4. Ensure your code follows the project's style.

5. Push the branch to your fork:
   ```
   git push origin your-branch-name
   ```

6. Create a Pull Request to the main repository.

7. In the Pull Request description, explain the changes made and why they're necessary.

## Code Style

- Use C# naming conventions:
  - PascalCase for class, method, and property names
  - camelCase for local variables and parameters
  - _camelCase for private fields

- Add comments for complex code sections or patching methods

- Follow SOLID and DRY principles

## Testing

Before submitting a Pull Request, ensure that:

1. Your code compiles without errors
2. You've manually tested the functionality
3. The patch works with the current version of WeMod
4. Changes don't break existing functionality

## License

By contributing, you agree that your contributions will be licensed under the [Apache License 2.0](LICENSE.md).

---

Thank you for contributing to the WeMod Patcher project!
