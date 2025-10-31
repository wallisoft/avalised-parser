#!/bin/bash

echo "╔════════════════════════════════════════╗"
echo "║  Building AVML Parser - Avalised 1.0   ║"
echo "╚════════════════════════════════════════╝"
echo ""

# Check for .NET
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found!"
    echo "Please install .NET 8.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ Package restore failed!"
    exit 1
fi

echo "✓ Packages restored"
echo ""

# Build
echo "🔨 Building project..."
dotnet build -c Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════╗"
echo "║  ✅ Build Successful!                  ║"
echo "╚════════════════════════════════════════╝"
echo ""
echo "To run the demo:"
echo "  dotnet run"
echo ""
echo "To parse your menu:"
echo "  dotnet run menu.avml /path/to/designer.db"
echo ""
echo "To see generated SQL:"
echo "  dotnet run menu.avml /path/to/designer.db --dry-run"
echo ""
