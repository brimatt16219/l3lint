# Processor profiles

Gateways and processors accept different amounts of enhanced data. One forwards at most 30 line items, another truncates purchase order numbers to 17 characters. A processor profile tells l3lint which limits apply, and which single-processor rules keep their full severity.

Select a profile with `--processor <name>`, `context.processor` in `.l3lint.yaml`, or `processor` in the MCP tool context. The default is `generic`. Every value in a shipped profile cites the public page it comes from; values that public pages do not state are left out, never guessed.

## What a profile changes

1. **Limits.** Rules that check counts and lengths read the profile's value when it sets one, and the rule's own default otherwise. GEN-FMT-005 checks the line-item count against `maxLineItems` (default 998). GEN-FMT-006 checks field lengths against the `...Max` limits. AMEX-FMT-002 checks `amexForwardedLineItems`.
2. **Single-processor rules.** Some requirements appear in only one processor's public documentation. Those rules name their profiles, and under any other profile their severity is capped at warning. Selecting the profile, or passing `--strict-profiles`, restores the catalog severity. See [Rules, severity and confidence](rules.md#single-processor-rules-and-profiles).

A profile can also disable rules or change severities, but none of the shipped profiles does.

## The shipped profiles

| Profile | Limits it sets | Single-processor rules it turns on |
| --- | --- | --- |
| `generic` | None; rule defaults apply | None |
| `stripe` | 200 line items; 4 forwarded to American Express; description 1,024; product code, commodity code and unit 12; postal code 10; customer reference 25 | None |
| `adyen` | Description 26; product code and commodity code 12; unit 3; customer reference 25; postal code 10 | None |
| `braintree` | 249 line items; description 35; product code, commodity code and unit 12; customer reference and PO number 17 | None |
| `cybersource` | PO number and customer reference 17; postal code 10 | None |
| `authorizenet` | 30 line items; description 31; commodity code and unit 12; invoice number 20; customer reference and PO number 25; postal code 20 | None |
| `checkoutcom` | 50 line items; 4 forwarded to American Express; description 26; product code, commodity code and unit 12; customer reference 15; postal code 10 | None |
| `jpmorgan` | 998 line items | MC-L3-008, MC-L3-009, MC-FMT-001, AMEX-FMT-001, DISC-FMT-001, DISC-L2-001 |
| `mastercard-gateway-amex` | Unit 2 (ANSI X12 codes); PO number 22; customer reference 20 | AMEX-L2-001, AMEX-L2-004, AMEX-L3-002 |
| `amex-advice-text` | None (invoice detail travels as up to 4 lines of 40 characters) | AMEX-FMT-001 |

Notes from the profile files:

- `stripe`: Stripe accepts descriptions up to 1,024 characters, but card networks show fewer; see VISA-L3-014. Customer references are truncated to 25 characters before they reach the networks. Only the first 4 line items reach American Express.
- `authorizenet`: the description limit is for the line item name; the separate description field holds 255.
- `checkoutcom`: descriptions are 12 characters for American Express, and the customer reference (`processing.order_id`) is 17 for American Express.
- `cybersource`: purchase order numbers are 17 characters at several processors and 25 at others. The canonical customer reference maps to the purchase order number.
- `mastercard-gateway-amex`: some settlement files keep only 17 characters of the customer reference.

`l3lint lint --processor nosuch` lists the names:

```text
$ l3lint lint samples/action-example/fixtures/payments/INV-20417.json --processor nosuch
error: Unknown processor profile "nosuch". Use one of: adyen, amex-advice-text, authorizenet, braintree, checkoutcom, cybersource, generic, jpmorgan, mastercard-gateway-amex, stripe, or a path to a profile file.
Run l3lint --help for usage.
```

## An example

A customer reference of 20 characters fits the generic limit of 25, but not Braintree's 17:

```text
$ sed 's/"reference": "PO-77812"/"reference": "PO-77812-WAREHOUSE-7"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only GEN-FMT-006 --quiet
1 transaction, 0 errors, 0 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
$ sed 's/"reference": "PO-77812"/"reference": "PO-77812-WAREHOUSE-7"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only GEN-FMT-006 --processor braintree
INV-20417  canonical  visa purchasing  MCC 5047  as of 2026-10-19
  warning     GEN-FMT-006        /customer/reference
           /customer/reference is 20 characters; the maximum is 17.
           Fix: Shorten it; otherwise it is truncated.
           Source: https://docs.stripe.com/payments/payment-line-items  (processor-doc)
  Likely tier: Product 3 (meets the documented requirement)

1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

The source shown is the rule's own source. The limit itself, 17, comes from the Braintree profile, whose source is Braintree's transaction sale reference.

## Your own profile

`--processor` also takes the path of a YAML file that matches [profile.schema.json](../schemas/profile.schema.json) (`l3lint schema profile` prints it). Relative paths resolve from the current folder for the flag, and from the config file's folder for `context.processor`.

```yaml
profile:
  id: my-gateway
  title: My gateway (Braintree limits)
  description: Line items and references as Braintree documents them.
  sources:
    - https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node
limits:
  maxLineItems:
    value: 249
    source: https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node
  customerReferenceMax:
    value: 17
    source: https://developer.paypal.com/braintree/docs/reference/request/transaction/sale/node
```

```text
$ sed 's/"reference": "PO-77812"/"reference": "PO-77812-WAREHOUSE-7"/' samples/action-example/fixtures/payments/INV-20417.json | l3lint lint - --only GEN-FMT-006 --processor my-gateway.yaml --quiet
1 transaction, 0 errors, 1 warning. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

Rules in a profile file:

- `profile.id`, `profile.title` and `profile.sources` are required. Every limit needs a `value` and a `source` URL that starts with `https://`; a `note` is optional.
- Limit names: `maxLineItems`, `amexForwardedLineItems`, `descriptionMax`, `productCodeMax`, `commodityCodeMax`, `unitOfMeasureMax`, `customerReferenceMax`, `purchaseOrderNumberMax`, `invoiceNumberMax`, `merchantTaxIdMax`, `postalCodeMax`, `requestorNameMax`, `taxTypeCodeMax`.
- `rules.disable` lists rule IDs or globs to skip, and `rules.severity` maps rule IDs to `error`, `warning` or `information`.
- A file that does not match the schema stops the run with exit code 3 and names the file and the failing path.

Single-processor rules list the IDs of the profiles they belong to (`jpmorgan`, `mastercard-gateway-amex`, `amex-advice-text`). A custom profile with another ID keeps them at warning; use `--strict-profiles` to run them at their catalog severity.

Profiles are data only. They cannot add checks or run code.
