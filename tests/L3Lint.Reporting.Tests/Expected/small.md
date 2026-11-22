## l3lint results

> Note: Payload values may appear in logs because --log-values is set.

3 transactions, 1 error, 2 warnings, 1 information, 1 input error. Rule packs: GEN 1.0.0, MC 1.0.0, VISA 1.0.0.

| Brand | Transactions | Errors | Warnings | Information |
| --- | ---: | ---: | ---: | ---: |
| mastercard | 1 | 0 | 0 | 1 |
| visa | 2 | 1 | 1 | 0 |
| Batch findings |  | 0 | 1 | 0 |
| **Total** | 3 | 1 | 2 | 1 |

1 transaction has no findings.

### Input errors

| Input | Line | Message |
| --- | ---: | --- |
| invoices/broken.json | 3 | invoices/broken.json is not valid JSON: a value is missing after the comma. |

### invoices/INV-20417.stripe.json

<details>
<summary><strong>INV-20417</strong>: 1 error, 1 warning</summary>

| Severity | Rule | Path | Message | Fix |
| --- | --- | --- | --- | --- |
| error | [VISA-L3-004](https://brimatt16219.github.io/l3lint/rules/VISA-L3-004.html "Commodity code is required for Product 3") | `/amount_details/line_items/1/payment_method_options/card/commodity_code` | Line 2 has no commodity code; Visa needs one on every line for Product 3. | Add the most specific 8-digit UNSPSC code for this item. |
| warning | [GEN-DQ-004](https://brimatt16219.github.io/l3lint/rules/GEN-DQ-004.html "Filler customer code or PO") | `/metadata/customer_reference` | "111111" at /customer/reference looks like filler, not the buyer's reference. | Ask the buyer for their real reference. |

**Likely tier:** below Product 3 (blocked by 1 error)

Estimated, using example-rates (effective 2026-10-01):

| Brand | Likely tier | Likely cost | Best tier | Best cost | Difference |
| --- | --- | ---: | --- | ---: | ---: |
| visa | level2 | 6.10 USD | level3 | 5.12 USD | 0.98 USD |

</details>

### invoices/batch 2.ndjson

<details>
<summary><strong>Transaction 2</strong>: 1 information</summary>

| Severity | Rule | Path | Message | Fix |
| --- | --- | --- | --- | --- |
| information | [GEN-DQ-001](https://brimatt16219.github.io/l3lint/rules/GEN-DQ-001.html) | `/lineItems/0/description` | Line 1 description "A\|B \<C\>" is a placeholder; use \*real\* text. | Use the real product or service name. |

**Likely tier:** Data Rate III (meets the documented requirement)

</details>

### Batch findings

| Severity | Rule | Input | Transaction | Path | Message | Fix |
| --- | --- | --- | --- | --- | --- | --- |
| warning | [GEN-DQ-007](https://brimatt16219.github.io/l3lint/rules/GEN-DQ-007.html "Same description or product code across most of a batch") | invoices/batch 2.ndjson | Transaction 2 | `/lineItems/0/description` | "A\|B \<C\>" appears at /lineItems/0/description on 2 of 2 transactions (100%). | Pull line data from the invoice, not a template. |
