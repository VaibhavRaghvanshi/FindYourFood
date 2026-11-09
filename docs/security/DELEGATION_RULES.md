# Delegation rules

The target hierarchy is Owner, CEO, Department Head, Regional or City Manager, Team Manager, and Employee. Membership by itself conveys only explicitly assigned permissions.

A grant must fit inside the grantor's own delegable permission set and resource scope. Evaluate reporting relationships and scope containment. Owner-only actions cannot be delegated by the CEO. Reject self-grants, role promotion through grant changes and attempts to administer a superior. Prevent hierarchy cycles. Sensitive grants require verified approval and an audit record of before/after state.

Revoking a parent grant revokes access derived solely from that grant. Use a transaction to persist the grant change and policy-version increment. Publish invalidation via the outbox. Test cross-tenant scope, self-escalation, delegation chains, concurrent revocation and unavailable authority.
