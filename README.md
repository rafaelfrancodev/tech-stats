# StatRecovery Console Application

## Overview

This console application processes ZIP files stored in an Amazon S3 bucket. Each ZIP file contains a CSV file that maps PDF file names to PO numbers. The application extracts the PDF files, uploads them back to the S3 bucket in a structured path, and maintains metadata to track processed files.

## Features

- Extracts PDF files from ZIP archives stored in an S3 bucket.
- Parses CSV files within the ZIP archives to map PDF file names to PO numbers.
- Uploads extracted PDF files back to the S3 bucket with a structured path: `by-po/{po-number}/{original-file-name}.pdf`.
- Maintains metadata to track which ZIP files and PDF files have been processed.
- Stores processing metadata in the same S3 bucket.

## Prerequisites

- .NET 8.0 SDK
- AWS S3 bucket with appropriate access permissions
- AWS credentials configured in `appsettings.json`

## Configuration

Create an `appsettings.json` file in the `StatRecovery.ConsoleApp` project directory with the following structure:

```json
{
    "AWS": {
        "AccessKeyId": "YOUR_ACCESS_KEY_ID",
        "SecretAccessKey": "YOUR_SECRET_ACCESS_KEY",
        "Region": "YOUR_AWS_REGION",
        "BucketName": "YOUR_BUCKET_NAME"
    },
    "MaxParallelFiles": 5,
    "MaxParallelUpload": 10
}
```

Replace the placeholders with your actual AWS credentials and bucket information.

## Building and Running

1. Clone the repository:
    ```sh
    git clone https://github.com/rafaelfrancodev/tech-stats.git
    cd StatRecovery
    ```

2. Build the solution:
    ```sh
    dotnet build
    ```

3. Run the console application:
    ```sh
    dotnet run --project StatRecovery.ConsoleApp
    ```

## Running Unit Tests

1. Navigate to the `StatRecovery.UnitTests` directory:
    ```sh
    cd StatRecovery.UnitTests
    ```

2. Run the tests:
    ```sh
    dotnet test
    ```

## Project Structure

- `StatRecovery.ConsoleApp`: The main console application project.
- `StatRecovery.Core`: Contains core services, interfaces, and models.
- `StatRecovery.Infrastructure`: Contains infrastructure services for interacting with AWS S3 and handling metadata.
- `StatRecovery.Application`: Contains application use cases and interfaces.
- `StatRecovery.UnitTests`: Contains unit tests for the application.

## Key Classes and Interfaces

- `IZipService`: Interface for extracting PDF files from ZIP archives.
- `IMetadataService`: Interface for loading and saving processing metadata.
- `IS3StorageService`: Interface for interacting with AWS S3.
- `CsvParserService`: Service for parsing CSV files.
- `ZipService`: Service for extracting PDF files from ZIP archives.
- `MetadataService`: Service for handling processing metadata.
- `S3StorageService`: Service for interacting with AWS S3.

## Logging

The application uses `Microsoft.Extensions.Logging;` for logging. Logs are output to the console.

## Error Handling

The application includes basic error handling to log and handle exceptions during file processing and S3 interactions.

## Contact

For any questions or issues, please contact rafael.apfsantos@gmail.com.

---