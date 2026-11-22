# L3Lint.Adapters

l3lint input adapters. They read Stripe, Adyen, Braintree, Cybersource, Authorize.net and Checkout.com request bodies, and CSV files with a YAML column map, and map them to l3lint's canonical payload. Each adapter keeps a path map, so findings point at fields in your own format.

```text
dotnet add package L3Lint.Adapters
```

`L3Lint.Hosting` already includes this package.

Documentation: https://brimatt16219.github.io/l3lint/articles/adapters.html
