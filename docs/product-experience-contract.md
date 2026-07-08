# KeyInventory UX / Product Experience Contract

## Product Character

KeyInventory is a professional enterprise internal web application. It is not a developer tool, demo, markdown browser, or framework-default scaffold.

## Shell

- Server-rendered ASP.NET Core Razor Pages.
- Persistent top navigation after authentication.
- Primary modules: Home, Admin, Operations, Reports, Help.
- Anonymous users see only a focused login surface.
- Ordinary operators do not see Admin configuration.

## Role-Aware Home

Home shows quick actions and summary cards based on capabilities:

- Assign, return, and audit actions for operators.
- Configuration cards for administrators.
- Read-only summary and reports for viewers.

## Module Separation

- Admin owns configuration and future user management.
- Operations owns daily workflows.
- Reports owns read-heavy analysis.
- Help owns in-app guidance.

## Integrated Help Center

- Help content is rendered in-app as HTML.
- Contextual links appear on relevant pages.
- Raw markdown downloads are forbidden.

## UI Requirements

- Bootstrap-style enterprise design system.
- Professional page headers.
- Cards for dashboards and module landing pages.
- Tables with filters, pagination, empty states, and responsive behavior.
- Badges for status, condition, and access level.
- Success toasts after confirmed server mutations.
- Field-level and summary validation messages.
- No default framework pages visible in the product experience.
- No developer UI, raw JSON viewers, or unstyled tables.

## Accessibility Baseline

- Semantic headings.
- Labels for form controls.
- Keyboard-friendly tab order.
- Focus management on validation failure.
- Color is not the only status indicator.
- Tables have meaningful headers.
