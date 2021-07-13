# cmi-viaduc-backend

- [cmi-viaduc](https://github.com/SwissFederalArchives/cmi-viaduc)
  - [cmi-viaduc-web-core](https://github.com/SwissFederalArchives/cmi-viaduc-web-core)
  - [cmi-viaduc-web-frontend](https://github.com/SwissFederalArchives/cmi-viaduc-web-frontend)
  - [cmi-viaduc-web-management](https://github.com/SwissFederalArchives/cmi-viaduc-web-management)
  - **[cmi-viaduc-backend](https://github.com/SwissFederalArchives/cmi-viaduc-backend)** :triangular_flag_on_post:

# Context

The [Viaduc](https://github.com/SwissFederalArchives/cmi-viaduc) project includes 4 code repositories. This current repository `cmi-viaduc-backend` is the backend for order management, consultation requests, administrative access and other settings. It was developed using C#. It includes several services and two API's.
The other repositories include the applications _public access_ ([cmi-viaduc-web-frontend](https://github.com/SwissFederalArchives/cmi-viaduc-web-frontend)) and the _internal management_ ([cmi-viaduc-web-management](https://github.com/SwissFederalArchives/cmi-viaduc-web-management));  both are Angular applications that access basic services of another Angular library called [cmi-viaduc-web-core](https://github.com/SwissFederalArchives/cmi-viaduc-web-core).

![The Big-Picture](docs/imgs/context.svg)

> Note: A general description of the repositories can be found in the repository [cmi-viaduc](https://github.com/SwissFederalArchives/cmi-viaduc).

# Table of contents

- [Requirements / Limitations](docs/requirements.md)
- [Installation](docs/installation.md)
- [Architecture and components of the solution](docs/architecture.md)
- [Connection to AIS](docs/connection-ais.md)
- [Connection to Digital Repository](docs/connection-dir.md)

# Authors

- [CM Informatik AG](https://cmiag.ch)
- [Evelix GmbH](https://evelix.ch)

# License

GNU Affero General Public License (AGPLv3), see [LICENSE](LICENSE.TXT)

# Contribute

This repository is a copy which is updated regularly - therefore contributions via pull requests are not possible. However, independent copies (forks) are possible under consideration of the AGPLV3 license.

# Contact

- For general questions (and technical support), please contact the Swiss Federal Archives by e-mail at info@bar.admin.ch.
- Technical questions or problems concerning the source code can be posted here on GitHub via the "Issues" interface.
