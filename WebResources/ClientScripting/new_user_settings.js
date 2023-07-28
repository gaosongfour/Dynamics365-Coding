/**
 * @namespace CrmUserSettings
 */
var CrmUserSettings = CrmUserSettings || {};

CrmUserSettings.showBasicInfo = function () {
    const userSettings = Xrm.Utility.getGlobalContext().userSettings;

    //user name and id
    const userName = userSettings.userName;
    const userId = userSettings.userId;

    //user Lcid ex 1033=English
    const userLangId = userSettings.languageId;

    //user currency
    const userCurrency = userSettings.transactionCurrency;

    const result = `user name=>${userName}
    user Id=>${userId}
    user LangId=>${userLangId}
    user currency=>${userCurrency.name}|id=>${userCurrency.id}|entityType=>${userCurrency.entityType}`;

    Xrm.Navigation.openAlertDialog({ text: result }, { height: 600, width: 600 });
}


CrmUserSettings.showSecurityRoleInfo = function () {
    const userSettings = Xrm.Utility.getGlobalContext().userSettings;
    //user security role id and name
    let result = new Array();
    result.push(`user security role count=>${userSettings.roles.getLength()}`);
    userSettings.roles.forEach(role => {
        result.push(`security role name=>${role.name}|id=>${role.id}`);
    });

    Xrm.Navigation.openAlertDialog({ text: result.join('\n') }, { height: 600, width: 400 });
}