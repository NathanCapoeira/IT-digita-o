# IT Digitação 2.0

## Referências e recursos

As seis imagens de `docs/` são mockups de composição e identidade. Foram examinadas antes da implementação e não são renderizadas como telas. Os quatro PNGs de mascote com transparência e os ícones SVG originais fornecidos estão em `assets/mascote/` e `assets/icones/`. Os ícones SVG foram convertidos localmente para PNG para uso pelo Windows Forms, sem dependência de rede em tempo de execução.

## Alterações implementadas

- Tema institucional com vermelho, bordô, branco, preto suave, fundo frio, bordas, destaque, verde e dourado.
- Componentes reutilizáveis para botões, cartões de categoria, cartões de atividade e métricas.
- Início com escolha explícita de duração, banner e mascote.
- Biblioteca em cartões com categorias, filtro de dificuldade, busca combinada por título e escolha aleatória.
- Página por categoria com descrição, dificuldade, contagem de palavras, duração aproximada e ação Praticar.
- Editor com painéis nativos de referência e digitação, métricas, cronômetro monotônico de sessão, controles de fonte e conclusão de texto validada.
- Tela de conclusão acolhedora, guia visual ABNT2 e configurações locais em JSON.
- Recursos locais copiados para a pasta de saída Release.

## Limitações e validação pendente

Os 30 textos carregam e têm IDs únicos em dez categorias. A contagem atual é 49–70 palavras para iniciantes, 64–76 para intermediários e 69–81 para avançados. As duas últimas faixas não correspondem ao intervalo de conteúdo definido no prompt. Não foram inflados artificialmente porque o pacote não contém fontes factuais dos textos. Revisão e expansão editorial por educadores permanecem pendentes.

O motor Core já existente continua responsável pela validação de caracteres e foi ampliado para refletir acertos/erros por posição e tentativas. A experiência completa de edição Unicode, combinações de acento ABNT2, Delete/navegação e preservação de sessão precisa de teste funcional no Windows. Também permanecem testes visuais em 1024×768, 1366×768 e escalas 100/125/150%, teste offline, memória e inicialização. O ambiente de implementação não contém .NET SDK, MSBuild nem Windows, portanto não foi possível compilar ou executar o novo `.exe` aqui. Windows 7 e Windows 10 continuam sem validação real.

As imagens incluídas foram mockups e recursos visuais, não capturas de tela do aplicativo implementado. Não se afirma fidelidade visual validada sem executar a aplicação e comparar em Windows.
