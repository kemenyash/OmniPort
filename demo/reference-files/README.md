# OmniPort demo reference files

This folder contains a small cross-format demo dataset for OmniPort.

## Purpose

The files are prepared as two compatible schemas:

- `customer-source-*` - source-side field names
- `customer-target-*` - target-side field names

They describe the same records, but the column/property names differ so you can
demo template inference, field mapping, and format conversion.

## Suggested mapping

| Source field | Target field | Type |
| --- | --- | --- |
| `CustomerId` | `ClientCode` | Integer |
| `FullName` | `DisplayName` | String |
| `Email` | `EmailAddress` | String |
| `IsActive` | `Enabled` | Boolean |
| `SignupDate` | `RegisteredAt` | DateTime |
| `BalanceUsd` | `CreditLimit` | Decimal |
| `CountryCode` | `RegionCode` | String |

## Included formats

- `.csv`
- `.json`
- `.xml`
- `.xlsx`

## XML note

XML files use the `<record>` node structure expected by the current OmniPort XML
parser.
