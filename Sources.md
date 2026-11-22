# Sources

Sources consulted for this spec, as of Oct 19, 2026. All are public; none are card-brand member documents. Five were read in full (Payroc, weAudit, Stripe, J.P. Morgan, Mastercard Gateway); the rest were read through search excerpts, so backlog task T-46 re-reads each one before v1.0.

| Source | Type | Used for |
| --- | --- | --- |
| [Payroc: Visa's new CEDP program](https://blog.payroc.com/what-you-need-to-know-about-visas-new-cedp-program) | Processor | CEDP timeline, the 12 Product 3 fields, placeholder ban, verification |
| [Versapay: CEDP](https://www.versapay.com/cedp) | Payments platform | Explicit tax exemption, generic descriptions, Level 2 rate alignment |
| [Versapay: CEDP guide](https://www.versapay.com/resources/visa-commercial-enhanced-data-program-guide) | Payments platform | Announcement date |
| [Nuvei: Visa CEDP guide](https://www.nuvei.com/posts/visa-cedp-commercial-enhanced-data-program) | Processor | Fee start, enforcement dates |
| [Spreedly: What is Visa CEDP?](https://www.spreedly.com/blog/what-is-visa-cedp-commercial-enhanced-data-program) | Payments platform | Fee applies regardless of validation |
| [Checkout.com: Changes to commercial interchange](https://support.checkout.com/hc/en-us/articles/30007033714194) | Gateway | October 2025 and April 2026 dates, verified status |
| [TabaPay: CEDP 2026 update](https://developers.tabapay.com/docs/commercial-enhanced-data-program-2026-update) | Processor | Verification pause and resumption, exclusion process, Product 2 sunset |
| [PayPal: Visa's CEDP](https://paypal.com/us/brc/article/visas-commercial-enhanced-data-program) | Processor | CEDP flag, 5 bps fee, eligible cards, Large Ticket estimate |
| [weAudit: Level 3 processing](https://www.weaudit.com/level-3-credit-card-processing/) | Audit firm | Product 3 and CNP rates, verification thresholds, Mastercard unit change |
| [Merchant Cost Consulting: CEDP rates](https://merchantcostconsulting.com/lower-credit-card-processing-fees/how-to-qualify-cedp-rates/) | Consultancy | Rejected descriptions, lagged credit timing |
| [Curbstone: CEDP data requirements](https://curbstone.com/new-data-requirements-cedp/) | Payments software | Machine-learning validation, reconciliation errors |
| [Finix: Visa programs changing](https://finix.com/resources/blogs/visa-level-2-3-programs-changing-cedp) | Processor | Merchant name and single-character descriptions rejected |
| [Verisave: Level 2 sunset](https://verisave.com/blog/visa-level-2-sunset-cedp-data-quality) | Consultancy | Generic descriptions trigger downgrades |
| [Forbes Business Council: CEDP](https://www.forbes.com/councils/forbesbusinesscouncil/2025/06/27/what-merchants-should-know-about-visas-commercial-enhanced-data-program/) | Article | Explicit tax requirement |
| [Epicor: Levels II and III](https://inte2.epicor.com/en/products/digital-commerce/payments/ptw-credit-card-processing/understanding-levels-2-and-3/) | Software vendor | Line-item formula, Mastercard Level 3 fields |
| [Cybersource community: Level 2 data](https://community.developer.cybersource.com/t5/Integration-and-Testing/Submitting-Level-2-Data/m-p/60293) | Gateway forum | Visa Level 3 minimum record fields |
| [Stripe: Payment line items](https://docs.stripe.com/payments/payment-line-items) | Gateway | Eligible cards, MCC exclusions, tax ranges, field limits, Amex 4-line limit |
| [Braintree: Level 2 and 3 required fields](https://developer.paypal.com/braintree/docs/reference/general/level-2-and-3-processing/required-fields/dotnet/) | Gateway | Tax ranges, total formula, allowed characters |
| [J.P. Morgan: Level 2 and Level 3 data](https://developer.payments.jpmorgan.com/docs/commerce/online-payments/capabilities/online-payments/payment-methods/cards/commercialcards) | Acquirer | Field requirements by brand, line-item limits, Mastercard units, Amex advice text |
| [Mastercard Gateway: Amex acquirer](https://developer.mastercard.com/mastercard-gateway/documentation/gateway-features/trans-enrich-data/order-and-line-item-data/level-2-and-3/level2-3-data-for-amex-acq/) | Gateway | Amex Level 2 and 3 conditions, lengths, unit codes |
| [Cybersource: unit of measure field](https://developer.cybersource.com/docs/cybs/en-us/api-fields/reference/all/so/api-fields/item-no-unit-of-measure.html) | Gateway | Unit lengths by processor and brand |
| [Cybersource: Level II and III intro](https://developer.cybersource.com/docs/cybs/en-us/level-2-3/developer/ctv/rest/level-2-3/l23-intro-overview.md) | Gateway | Amex Level 2 approval |
| [Checkout.com: Submit Level 2 or 3 data](https://www.checkout.com/docs/payments/manage-payments/submit-level-2-or-level-3-data) | Gateway | Mastercard unaffected by CEDP, field names |
| [Payrix: Interchange data requirements](https://resource.payrix.com/docs/interchange-data-requirements) | Processor | Mastercard MBS and MEF ineligible for Level 3 |
| [Payrix: Understanding interchange data](https://resource.payrix.com/resources/understanding-interchange-data-requirements) | Processor | Visa commercial prepaid ineligible |
| [PaymentCloud: Level 2 data](https://pcdocs.paymentcloudinc.com/developer-docs/processing-payments/level2) | Processor | Mastercard order number requirement |
| [Navan: Level 3 data](https://navan.com/resources/glossary/level-3-data) | Expense platform | Mastercard unit-of-measure tightening |
| [Adobe Commerce: Levels of card transactions](https://experienceleague.adobe.com/en/docs/commerce-merchant-services/payment-services/reporting/levels-card-payment-transactions) | Commerce platform | Discover offers no Level 2 or 3 pricing |
| [PayPal help: Level 2 and 3 transactions](https://www.paypal.com/us/cshelp/article/paypal-advanced-checkout---what-are-level-2-and-3-card-payment-transactions-help984) | Processor | Amex Level 2 approval, Discover Level 1 only |
| [Adyen: Level 2/3 data](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3) | Gateway | Field names and validation behavior |
| [Adyen: Level 2/3 in webhooks](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data/l2-l3/l2-l3-details-in-webhooks) | Gateway | Fallback to Level 2 |
| [Adyen: Enhanced scheme data](https://docs.adyen.com/payment-methods/cards/enhanced-scheme-data) | Gateway | AVS for Visa discounts |
| [BlueSnap: Level 2/3 data](https://developers.bluesnap.com/docs/level-23-data) | Gateway | Format-only validation, capture overrides authorization |
| [Authorize.net schema: LineItemType](https://rubydoc.info/gems/authorizenet/AuthorizeNet/API/LineItemType) | Gateway SDK | Line-item field names |
| [Zuora: Level 2 and 3 on Checkout.com](https://docs.zuora.com/en-us/zuora-payments/manage-payment-gateway-integrations-and-payment-methods/set-up-payment-gateway-integrations/checkout.com-payment-gateway/level-2-and-level-3-data-processing-on-checkout.com) | Billing platform | Existing CEDP logic inside one connector |
| [Microsoft Learn: Build an MCP server](https://learn.microsoft.com/dotnet/ai/quickstarts/build-mcp-server) | Vendor docs | C# SDK, NuGet MCP packaging, `dnx` |
| [Microsoft Learn: Publish to the MCP Registry](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/publish-mcp-registry) | Vendor docs | `server.json`, `mcp-publisher` |
| [MCP changelog 2025-11-25](https://modelcontextprotocol.io/specification/2025-11-25/changelog) | Protocol spec | Tool input errors as tool execution errors |
| [MCP changelog, latest](https://modelcontextprotocol.io/specification/latest/changelog) | Protocol spec | Sessions removed from Streamable HTTP |
| [GitHub: SARIF support](https://docs.github.com/en/enterprise-server@3.1/code-security/code-scanning/integrating-with-code-scanning/sarif-support-for-code-scanning) | Vendor docs | SARIF subset and upload limits |
| [GitHub: Upload a SARIF file](https://docs.github.com/en/enterprise-server@3.16/code-security/how-tos/find-and-fix-code-vulnerabilities/integrate-with-existing-tools/upload-sarif-file) | Vendor docs | `upload-sarif@v4` |
