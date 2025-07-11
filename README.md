# folder-replicator

## Overview
The Folder Replicator is a C# application designed to replicate folders from a source path to a destination path. It provides a simple interface for users to specify the folders they wish to replicate and handles the replication process efficiently.

## Installation
1. Clone the repository:
   ```
   git clone https://github.com/yourusername/folder-replicator.git
   ```
2. Navigate to the project directory:
   ```
   cd folder-replicator
   ```

## Usage
To run the application, use the following command:
```
dotnet run --project folder-replicator.csproj --source <SourcePath> --destination <DestinationPath>
```

Replace `<SourcePath>` and `<DestinationPath>` with the actual paths you want to use.

## Features
- Specify source and destination paths for folder replication.
- Validate options to ensure paths are correct before replication.
- Efficiently replicate folder contents while preserving the directory structure.
