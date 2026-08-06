// This is a TextMate grammar distributed by `starry-night`.
// This grammar is developed at
// <https://github.com/boundaryml/textMate-baml>
// and licensed `apache-2.0`.
// See <https://github.com/wooorm/starry-night> for more info.
/**
 * @import {Grammar} from '@wooorm/starry-night'
 */

/** @type {Grammar} */
const grammar = {
  dependencies: ['source.baml-jinja'],
  extensions: ['.baml'],
  names: ['baml'],
  patterns: [{include: '#comment'}, {include: '#schema'}],
  repository: {
    arrow_return_type: {
      begin: '(?<=\\))\\s*(->)\\s*',
      beginCaptures: {1: {name: 'keyword.control.baml.arrow'}},
      end: '(?=\\{)',
      patterns: [{include: '#comment'}, {include: '#type_definition'}]
    },
    block_attribute: {
      patterns: [
        {
          begin: '(@{1,2}(?:check|assert))\\s*\\(\\s*(?=\\{\\{)',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          contentName: 'string.quoted.block.thing',
          end: '\\)\\s*',
          patterns: [{include: 'source.baml-jinja'}]
        },
        {
          begin: '(@{1,2}(?:check|assert))\\s*\\(([^,]+)?\\s*,\\s*(?=\\{\\{)',
          beginCaptures: {
            1: {name: 'entity.name.function.attribute'},
            2: {name: 'variable.parameter.checkName'}
          },
          contentName: 'string.quoted.block.thing',
          end: '\\)\\s*',
          patterns: [{include: 'source.baml-jinja'}]
        },
        {
          begin: '(@{1,2}(?:check|assert))\\(([^,]+)?\\s*,\\s*()',
          beginCaptures: {
            1: {name: 'entity.name.function.attribute'},
            2: {name: 'variable.parameter.checkName'}
          },
          contentName: 'string.quoted.block.thing',
          end: '\\)',
          patterns: [{include: 'source.baml-jinja'}]
        },
        {
          begin: '(@{1,2}\\w+)\\(#"',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '"#\\)',
          name: 'string.quoted.block.baml',
          patterns: [
            {include: '#comment'},
            {include: '#language_block_python'},
            {include: '#language_block_ts'},
            {include: '#key_value'},
            {include: '#block_string_pair'},
            {include: '#string_literal'},
            {match: '\\(', name: 'punctuation.section.parens.open'},
            {match: '\\)', name: 'punctuation.section.parens.close'}
          ]
        },
        {
          begin: '(@{1,2}\\w+)\\(#{1,3}"',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '"#{1,3}\\)',
          name: 'string.quoted.block.baml',
          patterns: [
            {include: '#comment'},
            {include: '#language_block_python'},
            {include: '#language_block_ts'},
            {include: '#key_value'},
            {include: '#block_string_pair'},
            {include: '#string_literal'},
            {match: '\\(', name: 'punctuation.section.parens.open'},
            {match: '\\)', name: 'punctuation.section.parens.close'}
          ]
        },
        {
          begin: '(@{1,2}\\w+)\\(',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '\\)',
          patterns: [
            {include: '#string_unquoted'},
            {include: '#comment'},
            {include: '#language_block_python'},
            {include: '#language_block_ts'},
            {include: '#key_value'},
            {include: '#block_string_pair'},
            {include: '#string_literal', name: 'string.quoted.double'},
            {match: '\\(', name: 'punctuation.section.parens.open'},
            {match: '\\)', name: 'punctuation.section.parens.close'}
          ]
        },
        {
          begin: '(@{1,2}\\w+)\\("',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '"\\)',
          patterns: [{include: '#string_literal', name: 'string.quoted.double'}]
        },
        {
          begin: '(@{1,2}\\w+)\\(',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '\\)',
          patterns: [{match: '\\w+', name: 'string.unquoted'}]
        },
        {
          begin: '(@{1,2}\\w+)\\(#{1,3}',
          beginCaptures: {1: {name: 'entity.name.function.attribute'}},
          end: '#{1,3}\\)',
          name: 'string.quoted.block.baml',
          patterns: [
            {match: '\\\\.', name: 'constant.character.escape'},
            {begin: '\\(', end: '\\)', name: 'meta.embedded.block_attribute'},
            {include: '#comment'},
            {include: '#language_block_python'},
            {include: '#language_block_ts'},
            {include: '#key_value'},
            {include: '#block_string_pair'},
            {include: '#string_literal'},
            {match: '.', name: 'text.plain'}
          ]
        }
      ]
    },
    block_string_pair: {
      begin: '(\\w+)?\\s+(#{1,3}("){1,3})',
      beginCaptures: {
        1: {name: 'variable.other.readwrite.block_string_pair'},
        2: {name: 'string.quoted.block.baml.startquote'}
      },
      contentName: 'string.quoted.block.baml.stringpair',
      end: '(("){1,3}#{1,3})',
      endCaptures: {1: {name: 'string.quoted.block.baml.endquote'}},
      patterns: [
        {include: '#curly_comment'},
        {match: '\\{#chat\\([^}]*\\)}', name: 'entity.name.type.chat'},
        {
          match: '\\{#[a-zA-Z_][a-zA-Z0-9_.()><]*}',
          name: 'keyword.special.string.code'
        }
      ]
    },
    comment: {
      patterns: [
        {match: '//.*', name: 'comment.line'},
        {
          begin: '///',
          end: '$',
          name: 'comment.block.documentation',
          patterns: [{match: '.*', name: 'comment.block.documentation'}]
        },
        {include: '#curly_comment'}
      ]
    },
    config_block: {
      begin:
        '(client|generator|retry_policy|printer|test)\\s*(<([^>]+)>)?\\s+(\\w+)\\s*\\{',
      beginCaptures: {
        1: {name: 'storage.type.declaration'},
        3: {name: 'storage.type.declaration'},
        4: {name: 'entity.name.type'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {include: '#block_attribute'},
        {include: '#type_builder'},
        {include: '#property_assignment_expression'}
      ]
    },
    constant_numeric: {match: '\\b\\d+\\b', name: 'constant.numeric'},
    curly_comment: {
      begin: '\\{//',
      end: '//}',
      name: 'comment.line.double-slash.baml',
      patterns: [
        {include: '#language_block_python'},
        {include: '#language_block_ts'}
      ]
    },
    dynamic_declaration: {
      begin: '(dynamic)\\s+(\\w+)\\s*\\{',
      beginCaptures: {
        1: {name: 'storage.type.dynamic'},
        2: {name: 'entity.name.type.dynamic'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {
          begin: '(\\w+)',
          beginCaptures: {1: {name: 'variable.other.readwrite.interface'}},
          end: '(?=$|\\n|@|\\}|/)',
          patterns: [{include: '#type_definition'}]
        },
        {match: '\\b[A-Za-z_][A-Za-z0-9_]*\\b', name: 'variable.other.field'},
        {include: '#block_attribute'}
      ]
    },
    enum_declaration: {
      begin: '(enum)\\s+(\\w+)',
      beginCaptures: {
        1: {name: 'storage.type.enum'},
        2: {name: 'entity.name.type'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {include: '#block_attribute'},
        {match: '\\b[A-Za-z_][A-Za-z0-9_]*\\b', name: 'variable.other.field'}
      ]
    },
    function_body: {
      begin: '(?<=\\{)\\s*',
      end: '(?=\\})',
      patterns: [
        {include: '#comment'},
        {include: '#block_attribute'},
        {
          patterns: [
            {
              captures: {
                1: {name: 'variable.other.readwrite.client'},
                2: {
                  patterns: [
                    {match: '\\w+', name: 'entity.name.other.client'},
                    {include: '#string_literal'}
                  ]
                }
              },
              match: '(client)\\s+(\\w+|"[^"]*")',
              name: 'meta.client.declaration'
            },
            {
              begin: '\\s+(prompt)\\s+(#{1,5})(")',
              beginCaptures: {
                1: {name: 'variable.other.readwrite.prompt'},
                2: {name: 'string.quoted.block.baml.prompt'},
                3: {name: 'string.quoted.block.baml.prompt'}
              },
              contentName: 'string.quoted.block.baml.prompt',
              end: '\\s*("\\2)',
              endCaptures: {0: {name: 'string.quoted.block.baml.prompt'}},
              patterns: [{include: 'source.baml-jinja'}]
            }
          ]
        }
      ]
    },
    function_declaration: {
      begin: '(function)\\s+(\\w+)',
      beginCaptures: {
        1: {name: 'storage.type.declaration.function'},
        2: {name: 'entity.name.function'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {include: '#function_parameters'},
        {include: '#arrow_return_type'},
        {include: '#function_body'}
      ]
    },
    function_declaration2: {
      begin:
        '(function)\\s+(\\w+)\\(([^)]*)\\)\\s*(->)\\s*([\\w\\s\\[\\]|,?]+)\\s+\\{',
      beginCaptures: {
        1: {name: 'storage.type.declaration.function'},
        2: {name: 'entity.name.function'},
        3: {name: 'variable.parameter.function'},
        4: {name: 'keyword.operator'},
        5: {name: 'support.type'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {
          captures: {
            1: {name: 'variable.other.readwrite.client'},
            2: {
              patterns: [
                {match: '\\w+', name: 'entity.name.other.client'},
                {include: '#string_literal'}
              ]
            }
          },
          match: '(client)\\s+(\\w+|"[^"]*")',
          name: 'meta.client.declaration'
        },
        {
          begin: '\\s+(prompt)\\s+(#{1,3}")',
          beginCaptures: {
            1: {name: 'variable.other.readwrite.prompt'},
            2: {name: 'string.quoted.block.baml.prompt'}
          },
          contentName: 'string.quoted.block.baml.prompt',
          end: '\\s*("#{1,3})',
          endCaptures: {1: {name: 'string.quoted.block.baml.prompt'}},
          patterns: [{include: 'source.baml-jinja'}]
        },
        {include: '#block_attribute'}
      ]
    },
    function_name_type: {
      patterns: [
        {
          captures: {1: {name: 'variable.other.readwrite.function_name'}},
          match: '(\\w+)\\s*:'
        },
        {include: '#type_definition'}
      ]
    },
    function_parameters: {
      begin: '\\(',
      contentName: 'function.params',
      end: '\\)',
      patterns: [{include: '#comment'}, {include: '#function_name_type'}]
    },
    interface_declaration: {
      begin: '(class|override)\\s+(\\w+)\\s*\\{',
      beginCaptures: {
        1: {name: 'storage.type.declaration.interface'},
        2: {name: 'entity.name.type'}
      },
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {
          begin: '(\\w+)',
          beginCaptures: {1: {name: 'variable.other.readwrite.interface'}},
          end: '(?=$|\\n|@|\\}|/)',
          patterns: [{include: '#type_definition'}]
        },
        {include: '#block_attribute'}
      ]
    },
    invalid_assignment: {
      match:
        '\\b[a-zA-Z_][a-zA-Z0-9_]*\\s+[a-zA-Z_][a-zA-Z0-9_]*\\s+[a-zA-Z_][a-zA-Z0-9_]*',
      name: 'invalid.illegal'
    },
    key_array_pair: {
      begin: '("\\w+"|\\b\\w+\\b)\\s+\\[',
      captures: {1: {name: 'variable.other.readwrite'}},
      contentName: 'variable.other.readwrite.array',
      end: '\\]',
      patterns: [
        {include: '#key_array_pair'},
        {include: '#string_quoted2'},
        {include: '#constant_numeric'}
      ]
    },
    key_boolean_pair: {
      captures: {
        1: {name: 'variable.other.readwrite'},
        2: {name: 'constant.language.boolean'}
      },
      match: '("\\w+"|\\b\\w+\\b)\\s+(\\btrue\\b|\\bfalse\\b)'
    },
    key_custom_string_pair: {
      captures: {
        1: {name: 'variable.other.readwrite.custom_string'},
        2: {name: 'string.unquoted'}
      },
      match: '("\\w+"|\\b\\w+\\b)\\s+((?!null)[^\\s\\[\\{]+)'
    },
    key_null_pair: {
      captures: {
        1: {name: 'variable.other.readwrite.null'},
        2: {name: 'constant.language.nil.null'}
      },
      match: '("\\w+"|\\b\\w+\\b)\\s+(\\bnull\\b)'
    },
    key_number_pair: {
      captures: {
        1: {name: 'variable.other.readwrite.number_pair'},
        2: {name: 'constant.numeric'}
      },
      match: '("\\w+"|\\b\\w+\\b)\\s+(\\b\\d+\\b)'
    },
    key_quoted_string_pair: {
      captures: {
        1: {name: 'string.quoted.double'},
        2: {name: 'string.quoted.double'}
      },
      match: '("[^"]+")\\s+("[^"]+")'
    },
    key_string_pair: {
      begin: '("\\w+"|\\b\\w+\\b)\\s+(")',
      beginCaptures: {
        1: {name: 'variable.other.readwrite.key_string_pair'},
        2: {name: 'string.quoted.double'}
      },
      end: '"',
      endCaptures: {0: {name: 'string.quoted.double'}},
      patterns: [
        {match: '\\\\.', name: 'constant.character.escape'},
        {match: '[^"\\\\]+', name: 'string.quoted.double'}
      ]
    },
    key_value: {
      begin: '\\s*\\{',
      end: '\\s*\\}',
      patterns: [
        {include: '#comment'},
        {include: '#property_assignment_expression'}
      ]
    },
    key_value_pair: {
      begin: '(\\w+)\\s*',
      beginCaptures: {1: {name: 'variable.other.readwrite.key_value_pair'}},
      end: '(?=\\n)',
      patterns: [{include: '#string_literal'}]
    },
    keyword: {
      patterns: [
        {match: '\\b(input|output)\\b', name: 'keyword.special.input-output'}
      ]
    },
    language_block_jinja: {
      begin: '(jinja)(#{1,3}")',
      beginCaptures: {1: {name: 'comment.line'}, 2: {name: 'string.quoted'}},
      contentName: 'source.baml-jinja.embedded',
      end: '\\s*("{1,3}#)',
      endCaptures: {1: {name: 'string.quoted'}},
      patterns: [{include: 'source.baml-jinja'}]
    },
    language_block_python: {
      begin: '(python)(#{1,3}")',
      beginCaptures: {1: {name: 'comment.line'}, 2: {name: 'string.quoted'}},
      contentName: 'source.python.embedded',
      end: '\\s*("{1,3}#)',
      endCaptures: {1: {name: 'string.quoted'}},
      patterns: [{include: 'source.python'}]
    },
    language_block_ts: {
      begin: '(typescript)(#{1,3}")',
      beginCaptures: {1: {name: 'comment.line'}, 2: {name: 'string.quoted'}},
      contentName: 'source.ts.embedded',
      end: '\\s*("{1,3}#)',
      endCaptures: {1: {name: 'string.quoted'}},
      patterns: [{include: 'source.ts'}]
    },
    nested_key_value: {
      begin: '("\\w+"|\\b\\w+\\b)\\s+\\{',
      captures: {1: {name: 'variable.other.readwrite.nested_key'}},
      contentName: 'variable.other.readwrite.nested',
      end: '\\}',
      patterns: [
        {include: '#comment'},
        {include: '#key_value'},
        {include: '#key_null_pair'},
        {include: '#key_string_pair'},
        {include: '#language_block_python'},
        {include: '#language_block_ts'},
        {include: '#block_string_pair'},
        {include: '#key_quoted_string_pair'},
        {include: '#key_number_pair'},
        {include: '#key_boolean_pair'},
        {include: '#key_array_pair'},
        {include: '#key_custom_string_pair'}
      ]
    },
    property_assignment_expression: {
      patterns: [
        {include: '#key_null_pair'},
        {include: '#language_block_python'},
        {include: '#language_block_ts'},
        {include: '#block_string_pair'},
        {include: '#key_value'},
        {include: '#comment'},
        {include: '#key_string_pair'},
        {include: '#key_quoted_string_pair'},
        {include: '#key_number_pair'},
        {include: '#key_boolean_pair'},
        {include: '#key_array_pair'},
        {include: '#key_custom_string_pair'},
        {include: '#nested_key_value'}
      ]
    },
    schema: {
      patterns: [
        {include: '#enum_declaration'},
        {include: '#interface_declaration'},
        {include: '#template_string_declaration'},
        {include: '#function_declaration'},
        {include: '#config_block'},
        {include: '#type_alias'},
        {include: '#function'},
        {include: '#language_block_python'},
        {include: '#language_block_ts'},
        {include: '#language_block_jinja'},
        {include: '#type_builder'}
      ]
    },
    single_variable_no_assignment: {
      match: '^\\s*\\w+\\b',
      name: 'variable.other.readwrite.single_var'
    },
    string_literal: {match: '"[^"]*"', name: 'string.quoted.double'},
    string_quoted2: {
      begin: '"',
      end: '"',
      name: 'string.quoted.double',
      patterns: [{match: '\\\\.', name: 'constant.character.escape'}]
    },
    string_unquoted: {match: '\\b[\\w-]+\\b', name: 'string.unquoted'},
    template_string_body: {
      begin: '\\s+(#{1,3})(")',
      beginCaptures: {
        1: {name: 'string.quoted.block.baml.body.start'},
        2: {name: 'string.quoted.block.baml.body.start'}
      },
      contentName: 'string.quoted.block.baml.body',
      end: '(?="\\1)',
      patterns: [{include: 'source.baml-jinja'}]
    },
    template_string_declaration: {
      begin: '(template_string)\\s+(\\w+)',
      beginCaptures: {
        1: {name: 'storage.type.declaration.function'},
        2: {name: 'entity.name.function'}
      },
      end: '^("#{1,3})',
      endCaptures: {1: {name: 'string.quoted.block.baml.end'}},
      patterns: [
        {include: '#comment'},
        {include: '#function_parameters'},
        {include: '#template_string_body'}
      ]
    },
    type_alias: {
      begin: '(type)\\s+(\\w+)\\s*(=)',
      beginCaptures: {
        1: {name: 'storage.type.declaration'},
        2: {name: 'entity.name.type'},
        3: {name: 'keyword.operator.assignment'}
      },
      end: '(?=$|\\n)',
      patterns: [
        {include: '#comment'},
        {
          begin: '(?<=\\=)\\s*',
          end: '(?=//|$|\\n)',
          patterns: [
            {include: '#type_definition'},
            {include: '#block_attribute'}
          ]
        }
      ]
    },
    type_builder: {
      begin: '(type_builder)\\s*\\{',
      beginCaptures: {1: {name: 'keyword.control.type_builder'}},
      end: '\\}',
      patterns: [
        {include: '#interface_declaration'},
        {include: '#enum_declaration'},
        {include: '#dynamic_declaration'},
        {include: '#type_alias'},
        {include: '#comment'}
      ]
    },
    type_definition: {
      patterns: [
        {
          match: '\\b(bool|int|float|string|null|image|audio)\\b',
          name: 'storage.type.baml'
        },
        {
          begin: '(map)\\s*<',
          beginCaptures: {1: {name: 'storage.type.baml'}},
          end: '>',
          patterns: [
            {include: '#type_definition'},
            {include: '#type_definition'}
          ]
        },
        {match: '\\b(true|false)\\b', name: 'constant.language.boolean'},
        {match: '\\w+', name: 'support.type'},
        {include: '#string_literal'},
        {match: '\\[\\]', name: 'keyword.control.baml'},
        {match: '\\?', name: 'keyword.control.baml'},
        {match: '\\|', name: 'keyword.control.baml'},
        {
          begin: '\\(',
          beginCaptures: {0: {name: 'keyword.control'}},
          end: '(\\))(\\[\\])*(\\?)?',
          endCaptures: {
            1: {name: 'keyword.control'},
            2: {name: 'keyword.control'},
            3: {name: 'keyword.control'}
          },
          patterns: [{include: '#type_definition'}]
        }
      ]
    }
  },
  scopeName: 'source.baml'
}

export default grammar
