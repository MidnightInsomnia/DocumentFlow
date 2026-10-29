# Final research summary

Corpus-v1 established the initial baseline. Corpus-v2 exposed a large lexical distribution shift; Rules v2 and Hybrid v2 were then developed using corpus-v1/v2 feedback and frozen. Corpus-v3 was created afterward as the independent final evaluation corpus.

On corpus-v3, Rules v1 classified only Unknown documents. Rules v2 added three correct Contract classifications but did not generalize its extraction lexicon to the new business wording: both rules strategies produced zero correct expected fields and no automatic completions. This is a strong limitation of finite label lexicons under lexical shift.

The frozen local LLM classifier generalized much better: Pure LLM, HC v1 and Hybrid v2 each reached 97.22% classification and classified all known documents correctly; two Unknown documents were classified as Act. The LLM extractor provided the only substantial field recovery and automatic completion, reaching 62.66% field accuracy and 43.06% exact match, but failed on 14/72 documents and had the highest latency. Related-contract extraction was its weakest field family.

HC v1 and Hybrid v2 removed LLM-extraction failures but their deterministic rule extractors recovered almost none of the new lexical style, leaving every document in NeedsReview. Hybrid v2 therefore did not deliver the development-corpus advantage on the independent final corpus. It retained excellent classification and zero failures, at the cost of very low exact match and no automation.

ScannedPdf was the slowest source for every strategy because OCR dominated latency. Pure LLM field accuracy was strongest on ScannedPdf in this synthetic sample, while Image exact match was lowest. Medium documents happened to score best for Pure LLM; because source, layout and content still vary jointly and n=72, this must not be read as a causal difficulty effect.

Pure LLM was not fully deterministic (69/72 structured equality). Both classifier-only hybrids were 72/72 deterministic across two runs. The Rules v2 versus Hybrid v2 paired exact comparison produced b=2, c=0 and McNemar exact p=0.5, providing no evidence of a significant exact-match difference.

No strategy, corpus annotation, prompt, OCR configuration, validator or router was modified after the first corpus-v3 results. Production remains unchanged on Rules v1; engineering integration is intentionally deferred.
