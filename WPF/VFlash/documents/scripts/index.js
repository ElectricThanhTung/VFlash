
function showContent(sender, link) {
    var navigation_list = document.getElementById("navigation_list");
    for(var i = 0; i < navigation_list.childElementCount; i++) {
        if(navigation_list.children[i].getAttribute('class') != '')
            navigation_list.children[i].setAttribute('class', '');
    }
    sender.setAttribute('class', 'active');
    document.getElementById('content_iframe').setAttribute('src', link); 
}
