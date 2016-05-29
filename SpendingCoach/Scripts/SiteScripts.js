$('.ListItemCategory').change(function () {
    var selected = $(this).val();
    var options = $('.ListItemNameOne optgroup');
    options.each(function () {
        if ($(this).attr('Label') != selected) {
            $(this).hide();
        } else {
            $(this).show();
            $('.ListItemNameOne').val($(this).children().first().val());
        }
    });
});

$('input:radio').change(function () {
    if ($(this).attr('id') == 'SuggestedInput') {
        $('.ListItemNameOne').prop('disabled',false)
        $('.ListItemNameTwo').prop('disabled',true);
    } else {
        $('.ListItemNameTwo').prop('disabled',false);
        $('.ListItemNameOne').prop('disabled',true);
    }
});



