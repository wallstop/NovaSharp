"""Standalone checkout entry point. The image installs its private reader here."""

from mcp_credentials import main

if __name__ == "__main__":
    main(standalone=True)
