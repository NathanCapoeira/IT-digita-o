# IT Digitação

Aplicativo educacional de treino de digitação para o Instituto de Transformação. A aplicação foi planejada para funcionar localmente, sem conta, telemetria, serviços externos ou conexão durante o uso.

## Estado atual

Esta é uma fundação funcional em desenvolvimento, não uma versão 1.0 pronta para uso em sala. Inclui estrutura WinForms, biblioteca local com 30 atividades em dez categorias, filtros, editor de digitação, métricas básicas e cronômetro que começa no primeiro caractere. Ainda faltam validação robusta da biblioteca, persistência das preferências, bloqueio completo dos caminhos de colagem e testes automatizados abrangentes.

## Requisitos

- Windows com .NET Framework 4.8 instalado. No Windows 7, é necessário Windows 7 SP1, suporte a SHA-2 e atualização de servicing stack, além das atualizações de plataforma exigidas pelo instalador do .NET Framework 4.8. Instale o runtime previamente usando os pacotes oficiais da Microsoft. A implantação x86 permite executar em Windows de 32 e 64 bits.
- Para compilar: Visual Studio ou Build Tools com MSBuild e .NET Framework 4.8 targeting pack.

Windows 7 SP1 pode executar .NET Framework 4.8 após as atualizações e requisitos de sistema publicados pela Microsoft. Essa compatibilidade não foi testada nesta implementação. O projeto não instala pré-requisitos durante a execução.

## Compilar

No Windows, execute `scripts\build-windows.cmd` ou abra `ITDigitacao.sln` no Visual Studio e compile a configuração Release. O fluxo GitHub Actions em `.github/workflows/build-windows.yml` realiza compilação em runner Windows. O executável, quando compilado, ficará em `src\ITDigitacao.App\bin\Release\IT-Digitacao.exe`.

Este ambiente de desenvolvimento não tem .NET SDK, MSBuild ou Mono, portanto nenhum executável foi produzido ou validado aqui. O workflow Windows ainda precisa rodar para confirmar a compilação.

## Conteúdo

As atividades ficam em `content/activities.json`, em UTF-8. Cada item inclui identificador, título, categoria, dificuldade, descrição, texto, pergunta opcional, alternativas, índice da resposta correta e referências. Os textos atuais são autorais e não possuem fontes externas consultadas; revisão factual por educadores e conferência das informações são pendentes antes de uso institucional.

## Métricas

PPM usa caracteres corretos divididos por cinco e pelo tempo efetivo em minutos, com os primeiros cinco segundos ignorados. Precisão considera todas as tentativas de caracteres, inclusive erros que depois são apagados. O progresso usa caracteres líquidos atualmente corretos. As métricas são provisórias e ainda precisam de testes unitários e revisão do motor.

## Recursos gráficos

`assets/logo-conceitual.png` é uma arte provisória gerada a partir da referência visual enviada. Não é o logotipo institucional original. Substitua pela arte oficial aprovada antes da distribuição. O ícone multi-resolução também está pendente.
