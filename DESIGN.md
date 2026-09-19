# Design notes

## Decisions

- **`Payment` (domain) vs DTOs.** The repository stores a `Payment` entity. `PostPaymentRequest`, `PostPaymentResponse` and `GetPaymentResponse` are API contracts at the controller boundary and are mapped to/from `Payment` explicitly. The store never depends on a transport type.
- **Card details as strings.** `CardNumber` and `Cvv` are strings: 19 digits overflow an `int`/`long`, and leading zeros matter. The stored/returned `CardNumberLastFour` is also a string (`"0012"` must not become `12`).
- **Validation split.** Single-field rules are data annotations (`[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`, currency allowlist). Cross-field rules (expiry in the future) live in `IValidatableObject.Validate`. `[ApiController]` turns failures into automatic 400 responses.
- **Expiry rule.** A card is valid through the end of its expiry month (first of next month minus one tick). Guard clauses in `Validate` prevent `new DateTime` from throwing on out-of-range month/year before the annotations can report them.
- **Bank seam.** `IBankService`/`BankService` isolate the simulator call. `BankService` posts `card_number`, `expiry_date` (`MM/YYYY`), `currency`, `amount`, `cvv` to the simulator and maps the response via `[JsonPropertyName]` because `authorization_code` is snake_case.
- **Bank failure mapping.** A non-success bank response (or unreachable simulator) surfaces as `HttpRequestException`. The controller catches it, stores the payment with `Rejected`, and returns 502 Bad Gateway. Authorized and Declined both return 201 with the stored response: the payment resource was created either way.

## Assumptions

- Only `GBP`, `USD` and `EUR` are accepted currencies.
- Full card number and CVV are never stored or returned; only the last four digits are exposed.

## Testing

- Controller tests use a `FakeBank` so they run without Docker and are deterministic.
- `BankServiceTests` are integration tests against the live simulator and require `docker compose up`.

## Known limitations

- The `Rejected`/502 branch is implemented but not covered by a controller test yet.
- Post/Get response DTOs are currently identical and duplicated; they're kept separate so either can evolve independently, at the cost of some duplication.
- Test output prints HTTPS redirection warnings (`WebApplicationFactory` has no HTTPS port); cosmetic, unfixed.
