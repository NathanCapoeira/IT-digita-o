# IT Digitação 3.0 — auditoria e etapa inicial

## Base e estado do ambiente

- Base recuperável: branch `work`, commit `d89c485` (base 2.0), worktree limpo antes destas alterações.
- Aplicativo existente: WinForms, .NET Framework 4.8, x86; motor em `src/ITDigitacao.Core` (`netstandard2.0`); biblioteca local em `content/activities.json`.
- Dados auditados: 30 IDs únicos, 10 categorias, 3 atividades em cada categoria.
- Faixas de conteúdo verificadas: Iniciante 49–70 palavras; Intermediário 64–76; Avançado 69–81. Os níveis Intermediário e Avançado não alcançam os mínimos especificados (80 e 160). Os 30 registros têm a lista `references` vazia; não acrescentei fontes inventadas nem reescrevi textos educativos nesta etapa.
- O projeto de regressão existente mira .NET 8. O ambiente desta tarefa não tem `dotnet`, MSBuild, `csc` ou Mono; não foi possível compilar nem executar testes C# ou abrir uma janela Windows. Portanto, nenhuma captura de execução foi criada.
- `node --check docs/prototipo-v3/app.js`, parsing XML dos três XAML e leitura JSON/contagem da biblioteca passaram.

O kit completo extraído está guardado em `docs/spec-v3`, `docs/references-v3`, `docs/defects-v3`, `docs/prototipo-v3` e `docs/assets-v3`. As pastas de referências e defeitos preservam todos os PNGs do pacote para comparação nas etapas seguintes.

## Achados técnicos e recuperação

| Achado | Evidência no código/fonte | Ação nesta etapa | Situação |
|---|---|---|---|
| O editor bloqueava Delete e o motor só removia do fim | `TypingKeyDown` suprimia `Keys.Delete`; `TypingChanged` só reconciliava o comprimento; `TypingEngine` só expunha `Backspace()` | Motor agora expõe `Edit` e `DeleteAt`; a área WinForms reconcilia inserção/remoção por posição e bloqueia `WM_PASTE` localmente | Implementado; regressões adicionadas, execução pendente por falta do SDK |
| Contagem de categorias não mudava com filtro de dificuldade | A contagem era calculada uma vez em `ShowLibrary`, enquanto `ApplyCategoryFilter` só alternava `Visible` | Contagem e visibilidade agora são recalculadas pelo filtro selecionado | Implementado; validação visual em Windows pendente |
| Níveis/tamanho e fontes dos 30 textos não atendem à especificação | Contagem de palavras do JSON: iniciante 49–70, intermediário 64–76, avançado 69–81; todos os arrays `references` estão vazios | Mantive os textos existentes e não inventei fontes. Requer revisão editorial com fontes verificáveis | Bloqueador de conteúdo para aprovação final; não afeta a tela inicial |
| Categoria vazia nas capturas | JSON contém 3 atividades para cada categoria e IDs/categorias são consistentes. `ShowCategory` usava `TableLayoutPanel` `AutoSize`/`Dock` sem `RowStyles` explícitos e calculava a largura dos cartões antes da primeira medição | Linhas agora têm altura automática explícita; largura dos cartões acompanha o painel após a medição | Correção aplicada; confirmação em execução Windows pendente |
| Hero/título/mascote ausentes em capturas | `Banner` combinava `Dock=Fill`, `Dock=Bottom` e `Dock=Right`; a execução Windows não está disponível para confirmar ordem final de layout | Banner WinForms agora separa título/legenda em linhas explícitas; a nova tela inicial usa `Grid` WPF, imagem local e texto real | Correção aplicada; confirmação em execução Windows pendente |
| Grande vão entre duração/CTA e cartões | Em `ShowHome`, a quarta linha (`CTA`) recebe `Percent, 100`, absorvendo todo o espaço excedente | O novo layout usa linhas `Auto` e CTA imediatamente após duração | Corrigido na nova tela |
| Texto dos cartões podia ser cortado | Cartão WinForms fixo em 300×76, título e corpo em um único `Label` sem wrapping explícito | Cartões da tela WPF têm texto separado, `TextWrapping` e largura adaptável | Corrigido na nova tela |
| Editor sobrepõe título/documentos | O defeito está nas capturas. O código usa painel, barra e documentos em `Dock` na mesma superfície; sem execução não se atribui a ordem de z-order como causa confirmada | Migração visual WPF preserva o motor; o editor será tratado após o portão de aprovação do início | Pendente |

### Decisão de camada visual

A separação permite avaliar WPF sem substituir o motor: `src/ITDigitacao.Wpf` é um executável de prévia em .NET Framework 4.8/x86 e referencia `ITDigitacao.Core`. O XAML resolve os problemas de medição/ordem com `Grid`, `UniformGrid`, estilos e `ResourceDictionary`. O WinForms existente continua como executável próprio durante esta etapa; não há WebView, serviço remoto nem dependência visual externa. WPF 4.8 é compatível com a intenção Windows 7 SP1/Windows 10, mas os pré-requisitos do Windows 7 e ambos os sistemas continuam sem validação neste ambiente.

## Matriz de rastreabilidade

| Tela | Referência | Componentes da referência | Recursos locais | Código previsto/implementado |
|---|---|---|---|---|
| Início (implementado nesta etapa) | `docs/references-v3/inicio.png`, `identidade.png` | Cabeçalho institucional, hero, mascote, seletor 5–30 min, CTA e três cartões | Logo original convertido para PNG, `boas-vindas.png`, ícones Biblioteca/Teclado/Relógio | `src/ITDigitacao.Wpf/MainWindow.xaml`, `MainWindow.xaml.cs`, `DesignSystem.xaml` |
| Biblioteca (aguarda aprovação do início) | `docs/references-v3/biblioteca.png`, `05-telas.png` | Navegação lateral, busca/filtros, dez categorias, ação aleatória | `docs/assets-v3/categorias/*`, `docs/assets-v3/icones/*`, mascote ensinando | A implementar depois da aprovação visual |
| Categoria (aguarda aprovação) | `docs/references-v3/05-telas.png` | Breadcrumb, arte/título e cartões de atividade | Recortes de categorias são provisórios e mantidos em `docs/assets-v3/categorias/*` | A implementar depois da aprovação visual |
| Editor (aguarda aprovação) | `docs/references-v3/05-telas.png` | Cabeçalho/relógio, barra de ferramentas, documentos e métricas | Ícones locais; motor existente | Motor: `src/ITDigitacao.Core/Typing/TypingEngine.cs`; tela a implementar depois da aprovação |
| Conclusão (aguarda aprovação) | `docs/references-v3/05-telas.png` | Mascote comemorando, resultado e ações | `docs/assets-v3/mascote/celebrando.png` | A implementar depois da aprovação visual |
| Guia (aguarda aprovação) | `docs/references-v3/guia-configuracoes.png` | Teclado ABNT2, legenda por dedo, postura e dicas | `docs/assets-v3/icones/teclado.svg`, poses do mascote | A implementar depois da aprovação visual |
| Configurações (aguarda aprovação) | `docs/references-v3/guia-configuracoes.png` | Leitura, preferências e ações | Logo e ícones locais | A implementar depois da aprovação visual |

Capturas em `docs/defects-v3/` são regressões de referência, não imagens usadas como interface. As dez artes de categoria foram inspecionadas; têm cantos arredondados e fundos coloridos que pertencem às ilustrações, são explicitamente provisórias e não são usadas como cartões ou fundos na tela inicial.

## Tela inicial — o que foi construído

- Cabeçalho com marca original do instituto, identificação do produto e ações visíveis para guia, configurações e saída.
- Hero vermelho em gradiente, título/subtítulo legíveis, grafismo vetorial discreto e pose de boas-vindas com transparência preservada.
- Opções de 5, 10, 15, 20, 25 e 30 minutos; 20 minutos inicia selecionado como no mockup. Seleção tem estado visual, teclado e nome acessível.
- CTA logo depois da escolha. Nesta prévia ele informa que o fluxo da biblioteca vem após a aprovação; não inicia cronômetro nem simula sessão.
- Três cartões com os textos completos Aprenda/Pratique/Evolua; rodapé institucional Offline.
- Conteúdo usa `ScrollViewer`; seis cartões em uma linha quando há largura, três colunas em janela mais estreita. Não usa imagem de mockup como fundo.
- Arquivos locais estão em `src/ITDigitacao.Wpf/Assets`; recursos são incorporados ao executável WPF.

## Testes e validação ainda necessários

**Executado:** inspeção das seis capturas de defeitos, das referências, das quatro poses do mascote, das dez artes de categoria e dos recursos usados; checagem estática de JSON/XAML/JavaScript.

**Adicionado, mas não executado:** regressões para `DeleteAt`, edição no cursor e preservação da precisão histórica em `tests/ITDigitacao.Core.Tests/TypingEngineTests.cs`.

**Pendente:** build Release dos dois executáveis; execução dos testes xUnit; navegação e teclado; captura real lado a lado em 1366×768 e 1024×768; redimensionamento/DPI; validação offline e em Windows 10. Windows 7 SP1 permanece não validado.

Use `scripts\build-windows.cmd` no Windows com Visual Studio Build Tools, carga de trabalho .NET desktop, MSBuild e Developer Pack .NET Framework 4.8. O script compila o aplicativo atual e a prévia WPF, executa os testes se o .NET SDK estiver instalado e abre `src\ITDigitacao.Wpf\bin\x86\Release\net48\IT-Digitacao-3.0.exe`.

Esta é uma prévia da tela inicial, não uma Release final do produto: as telas seguintes e a validação humana continuam bloqueadas até receber a captura real e a aprovação visual explícita desta etapa.
