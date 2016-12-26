function addOption(selectbox, text, value) {
    var optn = document.createElement("option");
    optn.text = text;
    optn.value = value;
    selectbox.options.add(optn);


}
function addOption_list() {

    for (var i = 1; i < 32; ++i) {
        addOption(document.register_form.day_list, i, i);
    }
}
