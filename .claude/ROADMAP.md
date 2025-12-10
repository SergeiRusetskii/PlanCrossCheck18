# ROADMAP — PlanCrossCheck

*Last Updated: 2025-12-10*

> 🗺️ Strategic development roadmap for PlanCrossCheck ESAPI plugin
>
> **Workflow:** IDEAS.md → ROADMAP.md → BACKLOG.md

---

## Version 1.7.0 — Enhanced Validation Coverage

**Focus:** Expand validation checks for comprehensive plan QA

### New Validators
- [ ] Beam geometry validation (collimator angles, gantry angles)
- [ ] MLC leaf position validation
- [ ] Field junction analysis for multi-field plans
- [ ] Isocenter position validation against structure geometry

### Improvements
- [ ] Enhanced dose calculation grid validation
- [ ] Better error messages with specific recommendations
- [ ] Add tolerance thresholds configuration

---

## Version 2.0.0 — User Configuration & Reporting

**Focus:** Make the tool configurable and add reporting capabilities

### Configuration System
- [ ] XML-based configuration file for validation rules
- [ ] Tolerance thresholds adjustable per institution
- [ ] Enable/disable individual validators
- [ ] Custom severity levels per check

### Reporting
- [ ] Export validation results to PDF report
- [ ] Export to CSV for analysis
- [ ] Add timestamp and user information to reports
- [ ] Plan comparison mode (validate multiple plans)

### UI Enhancements
- [ ] Filter results by severity
- [ ] Search/filter validation results
- [ ] Expandable detail sections for each result
- [ ] Add validation statistics summary

---

## Version 2.5.0 — Advanced Clinical Checks

**Focus:** Clinical protocol compliance validation

### Protocol Validation
- [ ] Treatment site-specific validation rules
- [ ] Dose constraint checking against protocol templates
- [ ] Fractionation scheme validation
- [ ] Treatment technique verification (IMRT, VMAT, 3D-CRT)

### Structure Set Validation
- [ ] Required structures presence check
- [ ] Structure naming convention validation
- [ ] Overlap detection between critical structures
- [ ] Structure volume calculations and ranges

---

## Version 3.0.0 — Integration & Automation

**Focus:** Integration with Eclipse workflow and automation

### Eclipse Integration
- [ ] Context menu integration in Eclipse
- [ ] Auto-validation on plan approval workflow
- [ ] Integration with Eclipse scripting events
- [ ] Batch validation for multiple plans

### Database Integration
- [ ] Store validation history in database
- [ ] Trend analysis over time
- [ ] Institution-wide validation statistics
- [ ] Compare validation results across patients

### API Extensions
- [ ] RESTful API for external systems integration
- [ ] Command-line interface for batch processing
- [ ] Plugin SDK for custom validators

---

## Future Considerations

### Performance
- [ ] Multi-threaded validation execution
- [ ] Caching for repeated validations
- [ ] Optimize dose data access

### Documentation
- [ ] Comprehensive user manual
- [ ] Developer guide for custom validators
- [ ] ESAPI best practices documentation
- [ ] Validation rule rationale documentation

### Testing
- [ ] Unit test coverage for all validators
- [ ] Integration tests with sample plans
- [ ] Performance benchmarking suite
- [ ] Automated regression testing

---

## Dependencies & Technical Debt

### External Dependencies
- [ ] Track ESAPI version compatibility (currently v16.1+)
- [ ] Monitor .NET Framework updates
- [ ] WPF modernization considerations

### Code Quality
- [ ] Refactor large validator classes
- [ ] Improve error handling patterns
- [ ] Add logging framework
- [ ] Code documentation improvements

---

*Roadmap items are prioritized by version and may be adjusted based on clinical needs and user feedback.*
