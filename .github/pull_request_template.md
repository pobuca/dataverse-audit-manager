## Summary

Describe the change and its user impact.

## Validation

- [ ] Release build succeeds
- [ ] Tool opens safely without an active Dataverse connection
- [ ] Long-running operations remain asynchronous
- [ ] Screenshots use synthetic data only
- [ ] Documentation and changelog are updated when applicable

## Dataverse safety

- [ ] No Dataverse writes were added or changed
- [ ] Dataverse writes are staged and explicitly confirmed
- [ ] Destructive behavior has a separate warning and defaults to cancel/no

Explain any checked write or deletion behavior:
