# Helvetic Labs — LangBridge 🔗

> **Status:** pre‑alpha (v0.0.1)

LangBridge makes it trivial to ask LLMs **structured questions** from C# code.

```csharp
// high‑level API preview
var answer = await LlmBridge.AnswerBooleanQuestionAsync(text, "Did the user cancel their subscription?", ct);
if (answer == YesNoUnknown.Yes) { /* … */ }
```

## Roadmap

* [ ] `AnswerBooleanQuestionAsync`
* [ ] `ExtractJsonAsync<T>()`

## License

MIT — see LICENSE file.
