(function () {
    'use strict';

    var DEBOUNCE_MS = 200;

    function debounce(fn, ms) {
        var timer = null;
        return function () {
            var args = arguments;
            var self = this;
            if (timer) {
                clearTimeout(timer);
            }
            timer = setTimeout(function () {
                timer = null;
                fn.apply(self, args);
            }, ms);
        };
    }

    function initCombobox(root) {
        if (root.dataset.comboboxInitialized === 'true') {
            return;
        }
        root.dataset.comboboxInitialized = 'true';

        var input = root.querySelector('[data-role="combobox-input"]');
        var hidden = root.querySelector('[data-role="combobox-value"]');
        var hiddenParts = Array.prototype.slice.call(root.querySelectorAll('[data-role="combobox-value-part"]'));
        var toggle = root.querySelector('[data-role="combobox-toggle"]');
        var panel = root.querySelector('[data-role="combobox-panel"]');
        var searchUrl = root.getAttribute('data-search-url');
        var valueField = root.getAttribute('data-value-field') || 'value';
        var valueSeparator = root.getAttribute('data-value-separator') || '|';
        var valueParts = valueField.split(valueSeparator);
        var displayFields = (root.getAttribute('data-display-fields') || 'label').split(',');
        var displaySeparator = root.getAttribute('data-display-separator') || ' · ';
        var secondaryFields = (root.getAttribute('data-secondary-fields') || '').split(',').filter(function (s) { return s.length > 0; });
        var allowCustomValue = root.getAttribute('data-allow-custom-value') === 'true';

        if (!input || !panel || !searchUrl) {
            return;
        }

        var activeIndex = -1;
        var options = [];
        var isOpen = false;
        var lastQuery = null;

        function setActive(index) {
            var items = panel.querySelectorAll('.searchable-combobox-option');
            items.forEach(function (item, idx) {
                if (idx === index) {
                    item.setAttribute('data-active', 'true');
                    item.scrollIntoView({ block: 'nearest' });
                } else {
                    item.removeAttribute('data-active');
                }
            });
            activeIndex = index;
        }

        function openPanel() {
            panel.setAttribute('data-open', 'true');
            isOpen = true;
        }

        function closePanel() {
            panel.removeAttribute('data-open');
            isOpen = false;
            activeIndex = -1;
        }

        function renderStatus(message) {
            panel.innerHTML = '<li class="searchable-combobox-status">' + escapeHtml(message) + '</li>';
        }

        function renderEmpty() {
            panel.innerHTML = '<li class="searchable-combobox-empty">No matches. Refine your search.</li>';
        }

        function renderOptions(items) {
            options = items || [];
            if (options.length === 0) {
                renderEmpty();
                return;
            }

            var html = '';
            for (var i = 0; i < options.length; i++) {
                var item = options[i];
                var primary = displayFields.map(function (f) { return item[f.trim()] || ''; }).filter(function (s) { return s.length > 0; }).join(displaySeparator);
                var secondary = secondaryFields.map(function (f) { return item[f.trim()] || ''; }).filter(function (s) { return s.length > 0; }).join(' · ');
                var value = valueParts.length > 1
                    ? valueParts.map(function (f) { return item[f.trim()] || ''; }).join(valueSeparator)
                    : item[valueField];
                html += '<li class="searchable-combobox-option" role="option" data-value="' + escapeAttr(String(value)) + '" data-index="' + i + '">';
                html += '<span class="searchable-combobox-option-primary">' + escapeHtml(primary) + '</span>';
                if (secondary) {
                    html += '<span class="searchable-combobox-option-secondary">' + escapeHtml(secondary) + '</span>';
                }
                html += '</li>';
            }
            panel.innerHTML = html;
        }

        function pickOption(index) {
            if (index < 0 || index >= options.length) {
                return;
            }
            var chosen = options[index];
            var partValues = valueParts.map(function (f) { return chosen[f.trim()] != null ? String(chosen[f.trim()]) : ''; });
            var combinedValue = partValues.length > 1 ? partValues.join(valueSeparator) : partValues[0];
            var display = displayFields.map(function (f) { return chosen[f.trim()] || ''; }).filter(function (s) { return s.length > 0; }).join(displaySeparator);

            if (hidden) {
                hidden.value = String(combinedValue != null ? combinedValue : '');
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }
            if (hiddenParts.length > 0) {
                hiddenParts.forEach(function (partEl) {
                    var idxAttr = partEl.getAttribute('data-part-index');
                    var fieldAttr = partEl.getAttribute('data-part-field');
                    var val = '';
                    if (fieldAttr) {
                        val = chosen[fieldAttr.trim()] != null ? String(chosen[fieldAttr.trim()]) : '';
                    } else if (idxAttr != null) {
                        var idx = parseInt(idxAttr, 10);
                        if (!isNaN(idx) && idx >= 0 && idx < partValues.length) {
                            val = partValues[idx];
                        }
                    }
                    partEl.value = val;
                    partEl.dispatchEvent(new Event('change', { bubbles: true }));
                });
            }
            input.value = display;
            closePanel();

            root.dispatchEvent(new CustomEvent('combobox:selected', {
                bubbles: true,
                detail: { value: combinedValue, parts: partValues, item: chosen }
            }));
        }

        function buildUrl(query) {
            var separator = searchUrl.indexOf('?') >= 0 ? '&' : '?';
            var url = searchUrl + separator + 'q=' + encodeURIComponent(query || '');
            var extraQuery = root.getAttribute('data-extra-query') || '';
            if (extraQuery) {
                url += '&' + extraQuery;
            }
            return url;
        }

        function fetchAndRender(query) {
            var extraQueryNow = root.getAttribute('data-extra-query') || '';
            var cacheKey = query + '|' + extraQueryNow;
            if (lastQuery === cacheKey) {
                openPanel();
                return;
            }
            lastQuery = cacheKey;
            renderStatus('Searching...');
            openPanel();
            fetch(buildUrl(query), { credentials: 'same-origin', headers: { 'Accept': 'application/json' } })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Search failed: ' + response.status);
                    }
                    return response.json();
                })
                .then(function (items) {
                    if (lastQuery !== cacheKey) {
                        return;
                    }
                    renderOptions(Array.isArray(items) ? items : []);
                    // Never auto-select first/only.
                    setActive(-1);
                })
                .catch(function () {
                    renderStatus('Unable to load matches. Try again.');
                });
        }

        var doSearch = debounce(function () {
            fetchAndRender(input.value.trim());
        }, DEBOUNCE_MS);

        input.addEventListener('input', function () {
            var typed = input.value.trim();
            if (hidden) {
                if (allowCustomValue) {
                    hidden.value = typed;
                } else {
                    hidden.value = '';
                }
                hidden.dispatchEvent(new Event('change', { bubbles: true }));
            }
            if (hiddenParts.length > 0) {
                hiddenParts.forEach(function (partEl) {
                    partEl.value = '';
                    partEl.dispatchEvent(new Event('change', { bubbles: true }));
                });
            }
            if (allowCustomValue) {
                root.dispatchEvent(new CustomEvent('combobox:custom', {
                    bubbles: true,
                    detail: { value: typed }
                }));
            }
            doSearch();
        });

        input.addEventListener('focus', function () {
            if (!isOpen) {
                fetchAndRender(input.value.trim());
            }
        });

        input.addEventListener('keydown', function (event) {
            if (event.key === 'ArrowDown') {
                event.preventDefault();
                if (!isOpen) {
                    fetchAndRender(input.value.trim());
                    return;
                }
                if (options.length === 0) {
                    return;
                }
                setActive(activeIndex + 1 >= options.length ? 0 : activeIndex + 1);
            } else if (event.key === 'ArrowUp') {
                event.preventDefault();
                if (!isOpen) {
                    return;
                }
                if (options.length === 0) {
                    return;
                }
                setActive(activeIndex <= 0 ? options.length - 1 : activeIndex - 1);
            } else if (event.key === 'Enter') {
                if (isOpen && activeIndex >= 0) {
                    event.preventDefault();
                    pickOption(activeIndex);
                } else if (allowCustomValue && hidden) {
                    hidden.value = input.value.trim();
                    hidden.dispatchEvent(new Event('change', { bubbles: true }));
                    root.dispatchEvent(new CustomEvent('combobox:custom', {
                        bubbles: true,
                        detail: { value: hidden.value }
                    }));
                    closePanel();
                }
            } else if (event.key === 'Escape') {
                if (isOpen) {
                    event.preventDefault();
                    closePanel();
                }
            }
        });

        if (toggle) {
            toggle.addEventListener('click', function (event) {
                event.preventDefault();
                if (isOpen) {
                    closePanel();
                } else {
                    input.focus();
                    fetchAndRender(input.value.trim());
                }
            });
        }

        panel.addEventListener('mousedown', function (event) {
            // Prevent input blur closing before click resolves.
            event.preventDefault();
        });

        panel.addEventListener('click', function (event) {
            var target = event.target.closest('.searchable-combobox-option');
            if (!target) {
                return;
            }
            var index = parseInt(target.getAttribute('data-index'), 10);
            if (!isNaN(index)) {
                pickOption(index);
            }
        });

        document.addEventListener('click', function (event) {
            if (!root.contains(event.target)) {
                closePanel();
            }
        });

        input.addEventListener('blur', function () {
            // Delay so option click handler still fires.
            setTimeout(function () {
                if (!root.contains(document.activeElement)) {
                    if (allowCustomValue && hidden) {
                        var typed = input.value.trim();
                        if (hidden.value !== typed) {
                            hidden.value = typed;
                            hidden.dispatchEvent(new Event('change', { bubbles: true }));
                            root.dispatchEvent(new CustomEvent('combobox:custom', {
                                bubbles: true,
                                detail: { value: typed }
                            }));
                        }
                    }
                    closePanel();
                }
            }, 100);
        });

        // Allow hosts to reset the widget.
        root.addEventListener('combobox:reset', function () {
            if (hidden) {
                hidden.value = '';
            }
            if (hiddenParts.length > 0) {
                hiddenParts.forEach(function (partEl) { partEl.value = ''; });
            }
            input.value = '';
            lastQuery = null;
            options = [];
            closePanel();
        });
    }

    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function escapeAttr(s) {
        return escapeHtml(s);
    }

    function initAll(scope) {
        var root = scope || document;
        var elements = root.querySelectorAll('[data-searchable-combobox]');
        elements.forEach(initCombobox);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initAll(); });
    } else {
        initAll();
    }

    window.KeyInventoryCombobox = {
        init: initAll
    };
})();
