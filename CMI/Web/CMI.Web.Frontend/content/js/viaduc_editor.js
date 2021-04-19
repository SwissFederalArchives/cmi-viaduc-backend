
var editing = {};

$(function () {
    toastr.options = {
        "closeButton": false,
        "debug": false,
        "newestOnTop": false,
        "progressBar": false,
        "positionClass": "toast-top-right",
        "preventDuplicates": false,
        "onclick": null,
        "showDuration": "300",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "fadeIn",
        "hideMethod": "fadeOut"
    };

    editing.editor = new Jodit('#htmlEditor', {
        toolbarButtonSize: "small",
        toolbarAdaptive: false,
        beautifyHTML: false,
        cleanHTML: {
            "timeout": 300,
            "removeEmptyElements": false,
            "fillEmptyParagraph": false,
            "replaceNBSP": false,
            "replaceOldTags": {
                "b": "strong"
            },
            "allowTags": false,
            "denyTags": false
        }
    });
    
    editing.qs = (function (a) {
        if (a == "") return {};
        var b = {};
        for (var i = 0; i < a.length; ++i) {
            var p = a[i].split('=', 2);
            b[p[0]] = (p.length == 1) ? "" : decodeURIComponent(p[1].replace(/\+/g, " "));
        }
        return b;
    })(window.location.search.substr(1).split('&'));

    editing.html = $('html');
    editing.body = $('body');
    editing.page = $('.container-main');
    editing.controls = $('.editing-controls');
    editing.editorContainer = $('.editor-container');
    editing.editableControlsTemplate = $('.editor-controls-template').html();
    editing.editableControlsContainer = $('.editor-editable-controls-container');

    editing.editables = [];
    editing.current = null;

    editing.page.find('#content-wrapper').attr(
        'data-editable',
        JSON.stringify({
            id: "page",
            mode: "html"
        })
    );

    editing.controls.find('.edit').click(function (evt) {
        evt.preventDefault();
        editing.setup();
    });
    editing.controls.find('.save').click(function (evt) {
        evt.preventDefault();
        var baseUrl = (editing.baseUrl || "/viaduc");
        baseUrl = baseUrl.endsWith("/") ? baseUrl : baseUrl + "/";
        var loc = window.location,
            apiUrl = baseUrl + "api/Static/UpdateContent?nonce=" + (new Date().getTime()),
            data = {
                url: loc.pathname,
                language: editing.html.attr('lang'),
                markup: encodeURIComponent(editing.page.html())
            };

        $.ajax({
            type: "POST",
            contentType: "application/json",
            url: apiUrl,
            data: JSON.stringify(data),
            success: function () {
                toastr["success"]("Speichern erfolgreich");
                editing.cleanup();
            },
            error: function (err) {
                toastr["error"]("Speichern fehlgeschlagen: " + JSON.stringify(err, null, 2));
            }
        });
    });
    editing.controls.find('.cancel').click(function (evt) {
        evt.preventDefault();
        editing.cleanup();
        document.location.reload();
    });

    editing.edit = function (editable) {
        if (editing.current != null) {
            editing.cancel(editing.current);
        }
        editing.current = editable;
        var opts = editable.options || {};
        opts.mode = opts.mode || "";

        console.log('editing.edit', editable.id, opts);
        editing.editableControlsContainer.addClass('editing');
        editing.editorContainer.addClass('editing');
        editable.element.addClass('editing');
        editable.controls.addClass('editing');

        editing.editor.setEditorValue(editable.element.html(), false);

        editing.adapt();

        if (opts.mode.indexOf("html") >= 0 && editing.editor.getMode() !== Jodit.MODE_WYSIWYG) {
            editing.editor.setMode(Jodit.MODE_WYSIWYG);
        } else if (opts.mode && opts.mode.indexOf("html") < 0 && editing.editor.getMode() !== Jodit.MODE_SOURCE) {
            editing.editor.setMode(Jodit.MODE_SOURCE);
        }
    };
    editing.save = function (editable) {
        // Bug fix: in code view, html ist not updated
        if (editing.editor.getMode() === Jodit.MODE_SOURCE) {
            editing.editor.setMode(Jodit.MODE_WYSIWYG);
        }
        var html = editing.editor.getEditorValue();
        // console.log('editing.save', editable.id, html);
        editable.element.html(html);
        editing.end(editable);
        if (html.indexOf("data-editable") >= 0) {
            editing.cleanup();
            editing.setup();
        }
    };
    editing.cancel = function (editable) {
        editing.end(editable);
    };
    editing.end = function (editable) {
        console.log('editing.end', editable.id);
        editing.editableControlsContainer.removeClass('editing');
        editing.editorContainer.removeClass('editing');
        editable.element.removeClass('editing');
        editable.controls.removeClass('editing');
        editing.current = null;
        editing.adapt();
    };

    editing.adapt = function () {
        editing.editables.forEach(function (editable) {
            var element = editable.element,
                elemO = editable.element.offset();
            if (!element.is(':visible')) {
                editable.controls.hide();
                if (editable == editing.current) {
                    editing.editorContainer.removeClass('editing');
                }
            } else {
                editable.controls.show();
                editable.controls.css({
                    left: elemO.left,
                    top: elemO.top
                });
                if (editable == editing.current) {
                    var ctrlO = editable.controls.offset(),
                        ctrlH = editable.controls.height(),
                        elemW = element.width();
                    editing.editorContainer.addClass('editing');
                    editing.editorContainer.css({
                        left: elemO.left,
                        top: ctrlO.top + ctrlH,
                        width: Math.max(400, elemW)
                    });
                }
            }
        });
    };

    editing.setup = function () {
        var currentId = 1;
        $('[data-editable]').each(function () {
            var element = $(this),
                options = element.attr('data-editable');

            if (options && _.isString(options)) {
                if (options.indexOf("{") >= 0) {
                    options = $.parseJSON(options);
                } else {
                    options = { id: options };
                }
            } else {
                options = {};
            }

            if (!options.id) {
                options.id = "" + currentId;
                currentId += 1;
            }

            var id = options.id,
                controls = $(editing.editableControlsTemplate).attr("id", "editable-controls-" + id),
                editable = {
                    id: id,
                    options: options,
                    element: element,
                    controls: controls
                };

            editing.editables.push(editable);
            editing.editableControlsContainer.append(controls);

            controls.find('.edit').click(function (evt) {
                evt.preventDefault();
                editing.edit(editable);
            });
            controls.find('.save').click(function (evt) {
                evt.preventDefault();
                editing.save(editable);
            });
            controls.find('.cancel').click(function (evt) {
                evt.preventDefault();
                editing.cancel(editable);
            });
        });
        editing.adapt();

        editing.body.addClass('editing');
        editing.controls.addClass('enabled');
    };

    editing.cleanup = function () {
        editing.editables.forEach(function (editable) {
            editable.element.removeClass('editing');
            editable.controls.remove();
        });
        editing.editables = [];
        editing.editableControlsContainer.removeClass('editing');
        editing.editorContainer.removeClass('editing');

        editing.body.removeClass('editing');
        editing.controls.removeClass('enabled');
    };

    $(window).resize(function () {
        editing.adapt();
    });
    $(document).click(function () {
        editing.adapt();
    });

    //editing.setup();
});

