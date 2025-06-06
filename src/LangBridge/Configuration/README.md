# LangBridge Configuration

## Example appsettings.json

```json
{
  "LangBridge": {
    "Models": [
      {
        "Role": "Reasoning",
        "Tag": "reasoning-primary",
        "Provider": "OpenAI",
        "Model": "gpt-4-turbo-preview",
        "ApiKey": "sk-..."
      },
      {
        "Role": "Tooling",
        "Tag": "tooling-primary",
        "Provider": "OpenAI",
        "Model": "gpt-3.5-turbo",
        "ApiKey": "sk-..."
      }
    ]
  }
}
```

## Environment Variables

API keys can also be set via environment variables:
- `LANGBRIDGE__MODELS__0__APIKEY`
- `LANGBRIDGE__MODELS__1__APIKEY`