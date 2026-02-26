with open("src/Aiursoft.CorpHome/Views/Home/Index.cshtml", "r") as f:
    content = f.read()

import re

# Add float-in to node-center, node-icon, node-label
content = content.replace(
    '<div class="node-base node-center"',
    '<div class="node-base node-center float-in"'
)
content = content.replace(
    '<div class="node-base node-icon"',
    '<div class="node-base node-icon float-in"'
)
content = content.replace(
    '<div class="node-label"',
    '<div class="node-label float-in"'
)

# Fix openwebui mobile fallback and add float-in to mobile text-center
mobile_original = """        <div class="diagram-mobile-fallback flex-wrap justify-content-center gap-4 py-4" style="display: none;">
            <div class="text-center">
                <img src="~/images/app_icons/authentik.webp" alt="Authentik" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Authentik</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/nextcloud.webp" alt="Nextcloud" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Nextcloud</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/immich.webp" alt="Immich" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Immich</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/openwebui.webp" alt="Open WebUI" width="48" height="48" class="rounded-3 mb-1" />
            <div class="text-center">
                <img src="~/images/app_icons/mailcow.webp" alt="Mailcow" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Mailcow</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/gitlab.webp" alt="GitLab" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">GitLab</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/template.webp" alt="Template" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Template</div>
            </div>
            <div class="text-center">
                <img src="~/images/app_icons/zot.webp" alt="Zot" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Zot</div>
            </div>
        </div>"""

mobile_new = """        <div class="diagram-mobile-fallback flex-wrap justify-content-center gap-4 py-4" style="display: none;">
            <div class="text-center float-in">
                <img src="~/images/app_icons/authentik.webp" alt="Authentik" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Authentik</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/nextcloud.webp" alt="Nextcloud" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Nextcloud</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/immich.webp" alt="Immich" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Immich</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/openwebui.webp" alt="Open WebUI" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Open WebUI</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/mailcow.webp" alt="Mailcow" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Mailcow</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/gitlab.webp" alt="GitLab" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">GitLab</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/template.webp" alt="Template" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Template</div>
            </div>
            <div class="text-center float-in">
                <img src="~/images/app_icons/zot.webp" alt="Zot" width="48" height="48" class="rounded-3 mb-1" />
                <div class="extra-small text-gray-400">Zot</div>
            </div>
        </div>"""

content = content.replace(mobile_original, mobile_new)

# Add IntersectionObserver to the end of the file
script = """
@section scripts {
    <script>
        document.addEventListener("DOMContentLoaded", function () {
            const observer = new IntersectionObserver((entries) => {
                let delay = 0;
                entries.forEach(entry => {
                    if (entry.isIntersecting && !entry.target.classList.contains('float-in-visible')) {
                        setTimeout(() => {
                            entry.target.classList.add('float-in-visible');
                        }, delay);
                        delay += 50; // stagger 50ms
                    }
                });
            }, {
                threshold: 0.1
            });

            document.querySelectorAll('.float-in').forEach(el => {
                observer.observe(el);
            });
        });
    </script>
}
"""

if "@section scripts" not in content and "@section Scripts" not in content:
    content += script
else:
    # Need to append into existing script section
    pass # we handle this later

with open("src/Aiursoft.CorpHome/Views/Home/Index.cshtml", "w") as f:
    f.write(content)

