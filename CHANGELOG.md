## v0.2.1 (2020-11-03)

### New Features

- Create Unit Tests so we can be confident the Console app is working #13
- Make error messages more verbose, so technicians can debug their yaml more easily #14
- Move in ConsoleMethods from Core #12
- Use NLog for logging, so that technicians can customise log output #11

### Bug Fixes

- Output to console does not work #6

### Maintenance

- Add issue templates #8

### Other

- Update to the latest version of Core #10
- 'Execute' should be the default method to save technicians from extra typing #7

## v0.2.0 (2020-10-02)

- The paradigm by which Processes are defined has changed from functional to procedural
- A 'Process' is now a 'Step'
- Many new General Steps have been added to support the new paradigm (e.g. SetVariable, GetVariable, MathOperator)
- The yaml specification has changed entirely so yaml from the previous versions will not work with this version (please see [readme](README.md))
- Idol processes have been removed. They will return again in a later version.

For further details, please see the following epics:

- [Version 0.2.0](https://gitlab.com/groups/reductech/-/epics/5)
- [Change to Procedural Paradigm](https://gitlab.com/groups/reductech/edr/-/epics/6)

### Maintenance

- Update to version 0.2.0 of Core and Nuix #4

### Other

- Adjust namespaces to match new hierarchy #2

## v0.1.0 (2020-03-13)

Initial release. Console application to integrate EDR connectors.