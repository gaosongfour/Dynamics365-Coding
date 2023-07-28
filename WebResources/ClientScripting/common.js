/**
 * @namespace CrmFormContext
 */
var CrmFormContext = CrmFormContext || {};

/**
 * set all the fields disabled/enabled on the form
 * @param {Object} executionContext 
 * @param {Array} excludeFieldArray 
 * @param {boolean} disabled 
 * @returns 
 */
CrmFormContext.setFormDisable = function (executionContext, excludeFieldArray = null, disabled = true) {
    const formContext = executionContext.getFormContext();
    const controls = formContext.ui.controls;
    if (controls.getLength() === 0) return;

    const controlTypeArray = ['standard', 'lookup', 'choices', 'choice'];
    controls.forEach(control => {
        if (controlTypeArray.includes(control.getControlType()) && !excludeFieldArray?.includes(control.getName())) {
            control.setDisabled(disabled);
        }
    })
}

/**
 * Get Signle Lookup Typed object
 * @param {*} formContext 
 * @param {string} lookupName 
 * @returns 
 */
CrmFormContext.getLookupValue = function (formContext, lookupName) {
    const lookupField = formContext.getAttribute(lookupName);
    if (lookupField == null) throw Error(`Lookup Field ${lookupName} not present on the form`);
    const lookupArray = lookupField.getValue();
    if (lookupArray && lookupArray[0]) {
        return lookupArray[0];
    } else
        return null;
}