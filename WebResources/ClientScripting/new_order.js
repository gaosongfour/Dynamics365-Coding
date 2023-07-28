///<reference path= "common.js"/>

/**
 * @namespace OrderContext
 */
var OrderContext = OrderContext || {};

/**
 * event handler for onload
 * @param {*} executionContext 
 */
OrderContext.onLoad = function (executionContext) {
    const formContext = executionContext.getFormContext();
    formContext.getAttribute("new_clientid")?.addOnChange(this.onChange_clientid);
}

/**
 * onchange event for accountid
 * @param {*} executionContext 
 */
OrderContext.onChange_clientid = function (executionContext) {
    const formContext = executionContext.getFormContext();
    const lookupEntity = CrmFormContext.getLookupValue(formContext, "new_clientid");
    if (lookupEntity) {
        console.log(`lookup id=>${lookupEntity.id};name=>${lookupEntity.name};entityType=>${lookupEntity.entityType};`);
    } else {
        console.log("lookup cleared");
    }
}

/**
 * button Submit onClick event,we pass "PrimaryControl" as ribbon button parameter
 * @param {} formContext 
 */
OrderContext.onClickBtnSubmit = function (formContext) {
    //check if the form has unsaved data
    if (formContext.data.getIsDirty()) {
        Xrm.Navigation.openAlertDialog({ text: "The form is changed, please save the form then try to submit again." }, { height: 200, width: 400 });
        return;
    }

    const entityId = formContext.data.entity.getId();

    Xrm.Navigation.openConfirmDialog({ text: "Would you like to submit this record?", confirmButtonLabel: "Submit", cancelButtonLabel: "Cancel" }).then(
        function (success) {
            if (success.confirmed) {
                Xrm.Utility.showProgressIndicator("In Progress");
                try {

                    const clientLookup = CrmFormContext.getLookupValue(formContext, "new_clientid");
                    if (clientLookup === null) throw new Error("Client is null");

                    OrderContext.retrieveClient(clientLookup.id)
                        .then(
                            function (clientEntity) {
                                const locked = clientEntity?.new_locked;
                                if (locked) {
                                    throw new Error("Operation failed as Client is locked");
                                }
                                else {
                                    return OrderContext.retrieveOrderItems(entityId);
                                }
                            }
                        )
                        .then(
                            function (result) {
                                if (result.entities.length > 0) {
                                    OrderContext.submitOrder(entityId);
                                } else {
                                    throw new Error("Operation failed, please add at least one order item");
                                }
                            }
                        )
                        .catch(
                            function (error) {
                                console.log("err catch");
                                Xrm.Utility.closeProgressIndicator();
                                Xrm.Navigation.openAlertDialog({ text: error?.message });
                            }
                        );

                } catch (error) {
                    Xrm.Utility.closeProgressIndicator();
                    Xrm.Navigation.openAlertDialog({ text: error?.message });
                }
            }
        },
        function (error) {
            console.log(error);
        });
}

/**
 * button Submit enable rule, we pass "PrimaryControl" as ribbon button parameter
 * @param {} formContext 
 */
OrderContext.enableRuleBtnSubmit = function (formContext) {
    const orderStatus = formContext.getAttribute("new_orderstatus")?.getValue();
    const formType = formContext.ui.getFormType();
    return formType !== 1 && orderStatus === 10;
}

OrderContext.retrieveClient = function (clientId) {
    return Xrm.WebApi.retrieveRecord("new_client", clientId, `?$select=new_locked`);
}

OrderContext.retrieveOrderItems = function (orderId) {
    const query = `?$select=new_orderitemid&$filter=_new_orderid_value eq ${orderId}`;
    return Xrm.WebApi.retrieveMultipleRecords("new_orderitem", query);
}

OrderContext.submitOrder = function (entityId) {
    //update the order status by calling webapi
    const data = {
        new_orderstatus: 100
    };
    const entityName = "new_order";
    Xrm.WebApi.updateRecord(entityName, entityId, data).then(
        function success(result) {
            Xrm.Utility.closeProgressIndicator();
            Xrm.Navigation.openAlertDialog({ text: "Operation succeeded" }).then(
                function () {
                    //refresh the page
                    Xrm.Navigation.openForm({ entityName: entityName, entityId: entityId });
                }
            );
        },
        function (error) {
            //plugin exception will arrive here if any
            Xrm.Utility.closeProgressIndicator();
            Xrm.Navigation.openAlertDialog({ text: error.message });
        }
    );
}