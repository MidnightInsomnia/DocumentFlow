# Final corpus-v3 holdout comparison

Corpus-v3 was frozen before any strategy run. The values below are the first complete official runs; no algorithm, prompt, OCR setting or corpus annotation was changed after results were observed.

## Overall

| Metric | Rules v1 | Rules v2 | Pure LLM | HC v1 | Hybrid v2 |
|---|---:|---:|---:|---:|---:|
| Classification | 16.67% | 20.83% | 97.22% | 97.22% | 97.22% |
| Field accuracy | 0.00% | 0.00% | 62.66% | 4.81% | 4.81% |
| Exact match | 16.67% | 16.67% | 43.06% | 13.89% | 13.89% |
| Automatic completion | 0.00% | 0.00% | 50.00% | 0.00% | 0.00% |
| NeedsReview | 100.00% | 100.00% | 30.56% | 100.00% | 100.00% |
| Failure | 0.00% | 0.00% | 19.44% | 0.00% | 0.00% |
| Completed / Review / Failed | 0 / 72 / 0 | 0 / 72 / 0 | 36 / 22 / 14 | 0 / 72 / 0 | 0 / 72 / 0 |
| Average / Median / P95, ms | 288.38 / 278 / 929 | 287.92 / 278 / 932 | 1594.07 / 1690.5 / 2432 | 511.71 / 511.5 / 1154 | 472.86 / 474 / 1110 |

Optional HE v1: classification 16.67%, fields 0%, exact 16.67%, completion 0%, review 100%, failure 0%, timing 280.83 / 269 / 904 ms.

## Per-source metrics

Each cell is `classification / fields / exact / completion / review / failure`; timing is Average / Median / P95 ms.

| Strategy | Source | Quality rates | Timing |
|---|---|---|---|
| Rules v1 | TextPdf | 16.67 / 0 / 16.67 / 0 / 100 / 0 | 1.12 / 1.5 / 2 |
| Rules v1 | ScannedPdf | 16.67 / 0 / 16.67 / 0 / 100 / 0 | 596 / 530 / 958 |
| Rules v1 | Image | 16.67 / 0 / 16.67 / 0 / 100 / 0 | 268 / 278 / 319 |
| Rules v2 | TextPdf | 20.83 / 0 / 16.67 / 0 / 100 / 0 | 1.46 / 2 / 3 |
| Rules v2 | ScannedPdf | 20.83 / 0 / 16.67 / 0 / 100 / 0 | 593.92 / 529 / 956 |
| Rules v2 | Image | 20.83 / 0 / 16.67 / 0 / 100 / 0 | 268.38 / 278 / 313 |
| Pure LLM | TextPdf | 95.83 / 63.29 / 45.83 / 45.83 / 33.33 / 20.83 | 1283.75 / 1419.5 / 1869 |
| Pure LLM | ScannedPdf | 100 / 67.53 / 45.83 / 58.33 / 25 / 16.67 | 1956.29 / 2036 / 2777 |
| Pure LLM | Image | 95.83 / 57.14 / 37.5 / 45.83 / 33.33 / 20.83 | 1542.17 / 1670 / 2137 |
| HC v1 | TextPdf | 95.83 / 4.29 / 12.5 / 0 / 100 / 0 | 221.42 / 228.5 / 238 |
| HC v1 | ScannedPdf | 100 / 5.63 / 16.67 / 0 / 100 / 0 | 818.62 / 749.5 / 1186 |
| HC v1 | Image | 95.83 / 4.48 / 12.5 / 0 / 100 / 0 | 495.08 / 511.5 / 551 |
| Hybrid v2 | TextPdf | 95.83 / 4.29 / 12.5 / 0 / 100 / 0 | 190.5 / 197.5 / 208 |
| Hybrid v2 | ScannedPdf | 100 / 5.63 / 16.67 / 0 / 100 / 0 | 772.92 / 706 / 1140 |
| Hybrid v2 | Image | 95.83 / 4.48 / 12.5 / 0 / 100 / 0 | 455.17 / 474 / 502 |

## Per-difficulty metrics

Each quality cell is `classification / fields / exact / completion / review / failure`.

| Strategy | Easy | Medium | Hard |
|---|---|---|---|
| Rules v1 | 16.67 / 0 / 16.67 / 0 / 100 / 0 | 16.67 / 0 / 16.67 / 0 / 100 / 0 | 16.67 / 0 / 16.67 / 0 / 100 / 0 |
| Rules v2 | 20.83 / 0 / 16.67 / 0 / 100 / 0 | 20.83 / 0 / 16.67 / 0 / 100 / 0 | 20.83 / 0 / 16.67 / 0 / 100 / 0 |
| Pure LLM | 95.83 / 54.43 / 20.83 / 45.83 / 33.33 / 20.83 | 100 / 78.48 / 62.5 / 66.67 / 25 / 8.33 | 95.83 / 54.67 / 45.83 / 37.5 / 33.33 / 29.17 |
| HC v1 | 95.83 / 4.55 / 12.5 / 0 / 100 / 0 | 100 / 5.56 / 16.67 / 0 / 100 / 0 | 95.83 / 4.29 / 12.5 / 0 / 100 / 0 |
| Hybrid v2 | 95.83 / 4.55 / 12.5 / 0 / 100 / 0 | 100 / 5.56 / 16.67 / 0 / 100 / 0 | 95.83 / 4.29 / 12.5 / 0 / 100 / 0 |

## Field metrics

Values are `Expected / Correct / Missing / Wrong / Unexpected / Accuracy`.

| Strategy | Number | Date | INN | KPP | Amount | RelatedContractNumber |
|---|---|---|---|---|---|---|
| Rules v1 | 59/0/59/0/0/0 | 58/0/58/0/0/0 | 19/0/19/0/0/0 | 10/0/10/0/0/0 | 52/0/52/0/0/0 | 10/0/10/0/0/0 |
| Rules v2 | 59/0/59/0/0/0 | 58/0/58/0/0/0 | 19/0/19/0/0/0 | 10/0/10/0/0/0 | 52/0/52/0/0/0 | 10/0/10/0/0/0 |
| Pure LLM | 59/39/14/6/3/62.90 | 58/43/14/1/4/69.35 | 19/16/3/0/1/80.00 | 10/8/2/0/6/50.00 | 52/38/14/0/7/64.41 | 10/2/5/3/4/14.29 |
| HC v1 | 59/0/59/0/0/0 | 58/0/58/0/0/0 | 19/0/19/0/0/0 | 10/10/0/0/0/100 | 52/0/52/0/0/0 | 10/0/10/0/0/0 |
| Hybrid v2 | 59/0/39/20/0/0 | 58/0/58/0/0/0 | 19/0/19/0/0/0 | 10/10/0/0/0/100 | 52/0/52/0/0/0 | 10/0/10/0/0/0 |

## Primary comparisons and paired analysis

- Rules v1 → Rules v2: classification +4.16 pp; field and exact accuracy unchanged.
- Rules v2 vs Hybrid v2: LLM classification raises classification by 76.39 pp, but the frozen v2 extractor does not recognize this corpus's field wording, so exact is 2.78 pp lower.
- Pure LLM vs Hybrid v2: Pure LLM has substantially higher field/exact/completion but a 19.44% failure rate and much higher latency.
- HC v1 and Hybrid v2 have identical aggregate quality here; v2 is modestly faster in this run.

Rules v2 vs Hybrid v2 ExactMatch: both exact 10, Rules-only exact 2 (`unknown-final-textpdf-004`, `unknown-final-image-012`), Hybrid-only exact 0, both wrong 60. McNemar exact: b=2, c=0, two-sided p=0.500000; this is not statistically significant.

## Wilson 95% intervals

Wilson score formula uses z=1.9599639845 and document-level n=72. Field accuracy has no binomial CI because fields within a document are not independent.

| Strategy | Classification | Exact | Completion | Failure |
|---|---|---|---|---|
| Rules v1 | 9.80–26.91 | 9.80–26.91 | 0.00–5.07 | 0.00–5.07 |
| Rules v2 | 13.05–31.57 | 9.80–26.91 | 0.00–5.07 | 0.00–5.07 |
| Pure LLM | 90.43–99.23 | 32.26–54.56 | 38.75–61.25 | 11.95–30.03 |
| HC v1 | 90.43–99.23 | 7.72–23.71 | 0.00–5.07 | 0.00–5.07 |
| Hybrid v2 | 90.43–99.23 | 7.72–23.71 | 0.00–5.07 | 0.00–5.07 |

## Determinism and failures

- Pure LLM structured determinism: 69/72. Differences: `invoice-final-textpdf-006`, `invoice-final-scannedpdf-009`, `invoice-final-scannedpdf-013`.
- HC v1 structured determinism: 72/72.
- Hybrid v2 structured determinism: 72/72.
- Pure LLM failures: invalid JSON 11, unsupported/missing field name 2, empty KPP 1. No timeout or HTTP/Ollama failures.
- Other official strategies: no processing failures.

The confusion matrices are preserved in each `evaluation-summary.json`. Pure LLM/HC/Hybrid v2 correctly classified all 60 known-type documents but classified two of twelve Unknown documents as Act. Rules v1 classified only the 12 Unknown documents correctly; Rules v2 additionally classified three Contracts correctly.
