# SimFell

A DPS (Damage Per Second) simulator for the game Fellowship.

SimFell is a work-in-progress .NET 9.0 console application that simulates combat rotations, area-of-effect abilities, and more. It provides a flexible foundation for building and comparing DPS strategies.

## Table of Contents

- [SimFell](#simfell)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Project Structure](#project-structure)
  - [Prerequisites](#prerequisites)
  - [Getting Started](#getting-started)
  - [Usage](#usage)
  - [Troubleshooting](#troubleshooting)
  - [Contributing](#contributing)
  - [Contact](#contact)
  - [Credits](#credits)
  - [😡 I hate SIMs](#-i-hate-sims)

## Features

| Feature                                           | Status        |
| ------------------------------------------------- | ------------- |
| General Rotation                                  | ✅ Implemented |
| Area of Effect                                    | ✅ Implemented |
| Rime                                              | ✅ Implemented |
| SimC-like Integration                             | ⚙️ Rough Draft |
| Rotation Opener                                   | 💡 To Discuss  |
| Gems, Armor, Relics, Multi-dotting, Tariq Support | 🚧 Not Started |

## Project Structure

```text
SimFell/
├── SimFell.sln               # Solution file
├── SimFell/                  # Main project directory
│   ├── Base/                 # Core simulation abstractions
│   ├── Heroes/               # Hero classes and abilities
│   ├── Logging/              # Logging utilities
│   ├── Sim/                  # Simulation engine
│   ├── SimFileParser/        # Parser for input files (Enums, Models)
│   ├── Configs/              # Project-specific configuration files
│   ├── Program.cs            # Entry point
│   └── SimFell.csproj        # Project file
├── Configs/                  # Global/shared configuration files
├── simulation.log            # Sample run output log
├── README.md                 # This documentation
└── LICENSE                   # License information
```

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A .NET-compatible IDE or editor (Visual Studio, Rider, VS Code, etc.)

> [!TIP]
> @Toonic: _I recommend using [Rider](https://www.jetbrains.com/rider/) because it will setup everything for you._

## Getting Started

1. Clone the repository:

   ```bash
   git clone https://github.com/your-username/SimFell.git
   cd SimFell
   ```

2. Build and run the project:

   - **Using an IDE**: Open `SimFell.sln`, set the `SimFell` project as the startup project, and run.

   - **From the command line**:

     ```bash
     # Restore dependencies and build
     dotnet restore
     dotnet build SimFell/SimFell.csproj

     # Run the simulator
     dotnet run --project SimFell/SimFell.csproj
     ```

## Usage

Currently, SimFell runs as a command-line tool and outputs results to both the console and the `simulation.log` file in the root directory.

Example:

```bash
dotnet run --project SimFell/SimFell.csproj
```

Future releases will include a graphical interface and additional configuration options.

## Troubleshooting

- **Missing .NET runtime**: Verify installation with `dotnet --version`; it should output `9.0.x`.
- **Build failures**: Try cleaning and restoring with:

  ```bash
  dotnet clean
  dotnet restore
  ```

- **No output or empty log**: Ensure your `Configs/` files (`*.simfell`) are correctly configured.

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork this repository.
2. Create a feature branch:

   ```bash
   git checkout -b feature/your-feature
   ```

3. Commit your changes:

   ```bash
   git commit -m "Add feature: ..."
   ```

4. Push to your branch:

   ```bash
   git push origin feature/your-feature
   ```

5. Open a Pull Request against `main`.

- Follow the existing C# coding conventions.
- Add unit tests for new features when applicable.

## Contact

You can catch us on the [Fellowship Discord](https://discord.gg/fellowship)!

## Credits

- [michaelsherwood](https://github.com/michaelsherwood) — Progress bar and pretty-print ideas.
- [EriiYenn](https://github.com/EriiYenn) — Initial Python project structure.

## 😡 I hate SIMs

[Please see the following link.](https://github.com/simulationcraft/simc/wiki/PremedititatedProvocation)
