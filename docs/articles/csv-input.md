# CSV input

Finance and accounts receivable teams often have invoices as a CSV export from an ERP, not as gateway requests. l3lint lints those exports directly. A YAML column map says which column holds each canonical field, and rows that share an invoice ID become one transaction.

## A first run

`fixtures/adapters/csv/01-erp-two-invoices.csv` holds two invoices, one row per line item:

```text
invoice_number,invoice_total,card_brand,po_number,tax_total,taxable_flag,freight,item_description,sku,unspsc,qty,uom,unit_price,line_discount,line_total
INV-50311,280.36,visa,PO-48820,17.36,Y,15.00,Nitrile gloves L box,GLV-NL-100,42132203,10,BX,12.50,5.00,120.00
INV-50311,280.36,visa,PO-48820,17.36,Y,15.00,Isolation gown blue case,GWN-IS-B50,42131606,2,CA,64.00,,128.00
INV-50312,94.16,visa,PO-48821,6.16,Y,,Surgical masks box,MSK-SG-050,42131713,4,BX,22.00,,88.00
```

Its map, `01-erp-two-invoices.map.yaml`:

```yaml
format: csv
delimiter: ","
groupBy: invoice_number
transaction:
  id: invoice_number
  amount: invoice_total
  currency: { const: USD }
  card.brand: card_brand
  card.product: { const: purchasing }
  merchant.mcc: { const: "5047" }
  merchant.name: { const: Example Medical Supply }
  customer.reference: po_number
  tax.amount: tax_total
  tax.indicator: { from: taxable_flag, map: { Y: taxable, N: exempt } }
  amounts.freight: { from: freight, default: 0 }
lineItems:
  description: item_description
  productCode: sku
  commodityCode: unspsc
  quantity: qty
  unitOfMeasure: uom
  unitCost: unit_price
  discountAmount: { from: line_discount, default: 0 }
  total: line_total
```

Lint it with `--map`. A `.csv` file with a map is detected as CSV; `--format csv` says so explicitly.

```text
$ l3lint lint fixtures/adapters/csv/01-erp-two-invoices.csv --format csv --map fixtures/adapters/csv/01-erp-two-invoices.map.yaml --as-of 2026-10-19
INV-50311  csv  visa purchasing  MCC 5047  as of 2026-10-19
  error       VISA-L3-011        /amounts/discount
           /amounts/discount is missing; Visa needs it for Product 3, 0 when it does not apply.
           Fix: Send 0 for each that does not apply.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
  error       VISA-L3-011        /amounts/duty
           /amounts/duty is missing; Visa needs it for Product 3, 0 when it does not apply.
           Fix: Send 0 for each that does not apply.
           Source: https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program  (processor-doc)
```

The output is shortened here. The full run reports both invoices and ends with:

```text
2 transactions, 4 errors, 5 warnings. Rule packs: GEN 1.0.0, VISA 1.0.0.
```

The export has no order discount or duty columns, so Visa reports them missing. The paths are canonical because no column holds those fields. Add the columns, or map them to constants such as `amounts.duty: { const: 0 }` when they truly never apply.

Set the map once in `.l3lint.yaml` with `inputs.csvMap` (and `inputs.format: csv`) so the command is just `l3lint lint invoices.csv`. Without a map, CSV input is a usage error:

```text
$ l3lint lint fixtures/adapters/csv/01-erp-two-invoices.csv --format csv
error: CSV input needs a column map. Pass --map with a YAML map file, or set inputs.csvMap in .l3lint.yaml.
Run l3lint --help for usage.
```

## The column map

The map is YAML and must match [csvmap.schema.json](../schemas/csvmap.schema.json) (`l3lint schema csvmap` prints it).

| Key | Required | Meaning |
| --- | --- | --- |
| `format` | yes | Always `csv`. |
| `groupBy` | yes | The column that identifies the transaction, usually the invoice number. |
| `delimiter` | no | One character. A comma by default. |
| `amountUnits` | no | `major` (12.50, the default) or `minor` (1250). |
| `transaction` | yes | Canonical transaction fields, written with dots: `tax.amount`, `card.brand`, `shipping.toPostalCode`. |
| `lineItems` | yes | Canonical line fields: `description`, `quantity`, `unitCost`, `total` and so on. |

The field names are the paths of the [canonical payload](../reference/canonical.md). Each field takes one of three forms:

| Form | Example | Meaning |
| --- | --- | --- |
| A column name | `customer.reference: po_number` | The cell of that column. |
| A constant | `currency: { const: USD }` | The same value for every transaction. |
| A column with a value map and a default | `tax.indicator: { from: taxable_flag, map: { Y: taxable, N: exempt } }` | The cell, translated through `map` when it matches a key. `default` applies when the cell is empty. A cell that matches no key passes through unchanged. |

Cells convert with the invariant culture: numbers like `12.50`, dates as `yyyy-MM-dd`, booleans as `true`, `false`, `Y`, `N`, `yes`, `no`, `1` or `0`, and enum values as the canonical names (`visa`, `purchasing`, `taxable`, `credit`). `amex.adviceText` takes all advice lines from one cell, separated by `|`.

## Grouping rows into transactions

- Rows with the same `groupBy` value form one transaction, in the order each value first appears in the file.
- Each row is one line item. Without a `sequence` column, lines are numbered by row order within the transaction.
- Transaction-level values must agree across a transaction's rows. An empty cell does not count as a disagreement. When two rows disagree, the field is left out and reported once as GEN-FMT-001 at the second row's cell:

```text
$ l3lint lint fixtures/adapters/csv/02-rows-disagree.csv --map fixtures/adapters/csv/02-rows-disagree.map.yaml --only GEN-FMT-001
INV-50311  csv  unknown  as of 2026-10-30
  error       GEN-FMT-001        row 3, column B (total)
           Rows 2 and 3 of transaction INV-50311 disagree on total; the value is left out until the rows agree.
           Fix: Correct the field named in the path.
           Source: https://json-schema.org/draft/2020-12/json-schema-core  (policy)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0.
```

Paths for CSV are cell labels: `row 3, column B (total)`. The row is the file line where the record starts (the header is line 1), and the column is the spreadsheet letter with the header name.

## Minor units

Some exports store amounts as integers in minor units: 1250 for 12.50 USD. Set `amountUnits: minor`. Amount columns (amounts, taxes, discounts, freight, duty, totals and unit cost) must then be whole numbers, and each is divided by 10 to the power of the transaction currency's ISO 4217 exponent. Quantities are not amounts and are read as written.

`fixtures/adapters/csv/03-minor-units-three-currencies.csv` holds one invoice each in USD (exponent 2), JPY (exponent 0) and BHD (exponent 3). The canonical form shows the conversion; here is the BHD invoice (the other two lines are left out):

```text
$ l3lint convert fixtures/adapters/csv/03-minor-units-three-currencies.csv --map fixtures/adapters/csv/03-minor-units-three-currencies.map.yaml --to canonical --canonical-only
{"schemaVersion":"1.0","id":"INV-BH-7733","amount":19.204,"currency":"BHD","customer":{"reference":"PO-BH-0412"},"order":{"invoiceNumber":"INV-BH-7733"},"tax":{"amount":0.829,"indicator":"taxable"},"amounts":{"discount":0.000,"freight":1.500,"duty":0.000},"shipping":{"fromPostalCode":"1001","toPostalCode":"317","toCountry":"BH"},"lineItems":[{"sequence":1,"description":"Cable ties black pack","productCode":"CBL-TIE-BK","commodityCode":"39121704","quantity":5,"unitOfMeasure":"PK","unitCost":2.750,"discountAmount":0.250,"taxAmount":0.660,"total":13.500},{"sequence":2,"description":"Electrical tape roll","productCode":"ETP-RL-19","commodityCode":"31201502","quantity":3,"unitOfMeasure":"RL","unitCost":1.125,"discountAmount":0.000,"taxAmount":0.169,"total":3.375}]}
```

## Cells that do not convert

A cell that does not convert to its field's type is one GEN-FMT-001 finding at that cell. The value is left out, and rules that need it skip it instead of reporting it missing.

```text
$ l3lint lint fixtures/adapters/csv/06-invalid-cells.csv --map fixtures/adapters/csv/06-invalid-cells.map.yaml --brand visa --as-of 2026-10-19 --only GEN-FMT-001
```

reports three findings (shortened here to two lines each):

```text
  error       GEN-FMT-001        row 2, column P (qty)
           row 2, column P (qty) must be a number such as 12.50.
  error       GEN-FMT-001        row 3, column U (line_total)
           row 3, column U (line_total) must be a number such as 12.50.
  error       GEN-FMT-001        row 2, column F (tax_flag)
           row 2, column F (tax_flag) must be one of taxable, exempt, notProvided.
```

The quantity cell held the word `ten`, the line total held a letter O for a zero, and the tax flag `X` matched no key of the value map, so it passed through and is not a tax indicator.

## Card numbers in cells

Every cell is checked for card numbers before mapping. A match is masked and reported at its cell:

```text
$ l3lint lint 05-card-number-in-cell.csv --map 05-card-number-in-cell.map.yaml --only GEN-PCI-001
INV-50311  csv  unknown  as of 2026-10-30
  error       GEN-PCI-001        row 2, column M (item)
           A card number was found at row 2, column M (item) and masked as ************1111.
           Fix: Remove the card number; send only the last four digits in card.last4.
           Source: https://www.pcisecuritystandards.org/standards/pci-dss/  (policy)

1 transaction, 1 error, 0 warnings. Rule packs: GEN 1.0.0.
```

(Run from `fixtures/adapters/csv/`.) `pci.allowFields` exempts a column when the canonical field it maps to matches an allowed pointer, for example `/lineItems/*/description`. Only add a path after a person has confirmed its values are not card numbers.

## Errors in the map or the file

These are input errors (exit code 2), and the message names the file:

- the map is missing, is not valid YAML, or does not match `csvmap.schema.json` (the message gives the JSON Pointer of the bad entry);
- the map names a column the file does not have;
- the file has no header row.

## Fixes and conversion

CSV cells have no JSON Pointer, so `l3lint fix` writes patches against the canonical form of each transaction, one line per transaction, and `--write` writes canonical NDJSON instead of a fixed CSV. `l3lint convert --canonical-only` writes the same canonical NDJSON without fixes. See [Fixes](fixes.md).
